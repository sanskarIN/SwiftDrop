using System.Text;
using SwiftDrop.Core.Diagnostics;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Storage;

namespace SwiftDrop.App.Services;

public sealed class TransferHistoryService
{
    public const string PrivacyRedactionMarker = "[private]";
    public const int DefaultPerformanceTrendWindowDays = TransferPerformanceTrendAnalyzer.DefaultWindowDays;
    private const string PerformanceTrendExportPattern = "swiftdrop-performance-trend-*.csv";
    private readonly TransferHistoryStore _store;
    private readonly AppSettingsService _settings;
    private readonly SemaphoreSlim _initializationGate = new(1, 1);
    private bool _initialized;

    public TransferHistoryService(AppSettingsService settings)
    {
        _settings = settings;
        _store = new TransferHistoryStore(Path.Combine(FileSystem.AppDataDirectory, "swiftdrop.db"));
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_initialized) return;
        await _initializationGate.WaitAsync(ct);
        try
        {
            if (_initialized) return;
            await _store.InitializeAsync(ct);
            _initialized = true;
        }
        finally
        {
            _initializationGate.Release();
        }
        await ApplyRetentionAsync(ct);
    }

    public async Task ApplyRetentionAsync(CancellationToken ct = default)
    {
        if (!_initialized)
        {
            await InitializeAsync(ct);
            return;
        }
        var days = _settings.Load().HistoryRetentionDays;
        if (days == 0)
        {
            await _store.ClearAsync(ct);
            return;
        }
        await _store.PruneOlderThanAsync(DateTimeOffset.UtcNow.AddDays(-days), ct);
    }

    public async Task AddAsync(
        string direction,
        string peerDeviceName,
        string fileName,
        long sizeBytes,
        string status,
        bool integrityVerified,
        CancellationToken ct = default,
        TimeSpan? duration = null,
        long? measuredBytes = null)
    {
        await InitializeAsync(ct);
        var settings = _settings.Load();
        if (settings.HistoryRetentionDays == 0) return;
        var storedPeer = settings.PrivacyMode ? PrivacyRedactionMarker : peerDeviceName;
        var storedName = settings.PrivacyMode ? PrivacyRedactionMarker : fileName;
        var measurement = TransferPerformanceAnalyzer.NormalizeOptionalMeasurement(duration, sizeBytes, measuredBytes);
        var entry = new TransferHistoryEntry(
            Guid.NewGuid().ToString("N"),
            direction,
            storedPeer,
            storedName,
            sizeBytes,
            DateTimeOffset.UtcNow,
            status,
            integrityVerified,
            measurement?.DurationMilliseconds,
            measurement?.MeasuredBytes);
        await _store.AddAsync(entry, ct);
    }

    public async Task<IReadOnlyList<TransferHistoryEntry>> GetRecentAsync(int limit = 100, CancellationToken ct = default)
    {
        await InitializeAsync(ct);
        var items = await _store.GetRecentAsync(limit, ct);
        if (!_settings.Load().PrivacyMode) return items;
        return items
            .Select(item => item with
            {
                PeerDeviceName = PrivacyRedactionMarker,
                FileName = PrivacyRedactionMarker
            })
            .ToArray();
    }

    public async Task<IReadOnlyList<TransferPerformanceTrendPoint>> GetPerformanceTrendAsync(
        int windowDays = DefaultPerformanceTrendWindowDays,
        CancellationToken ct = default)
    {
        ValidateTrendWindow(windowDays);
        await InitializeAsync(ct);

        var windowEndUtc = DateTimeOffset.UtcNow;
        var cutoffUtc = new DateTimeOffset(
            windowEndUtc.UtcDateTime.Date.AddDays(-(windowDays - 1)),
            TimeSpan.Zero);
        var entries = await _store.GetPerformanceEntriesSinceAsync(cutoffUtc, ct);
        return TransferPerformanceTrendAnalyzer.BuildDaily(entries, windowEndUtc, windowDays);
    }

    public async Task<string> ExportPerformanceTrendCsvAsync(
        int windowDays = DefaultPerformanceTrendWindowDays,
        CancellationToken ct = default)
    {
        var points = await GetPerformanceTrendAsync(windowDays, ct);
        var csv = TransferPerformanceTrendCsvExporter.Export(points);
        var cacheDirectory = FileSystem.CacheDirectory;
        CleanupPreviousPerformanceTrendExports(cacheDirectory);
        var fileName = $"swiftdrop-performance-trend-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmssfff}.csv";
        var path = Path.Combine(cacheDirectory, fileName);
        await File.WriteAllTextAsync(path, csv, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), ct);
        return path;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await InitializeAsync(ct);
        await _store.DeleteAsync(id, ct);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        await InitializeAsync(ct);
        await _store.ClearAsync(ct);
    }

    private static void ValidateTrendWindow(int windowDays)
    {
        if (windowDays is < 1 or > TransferPerformanceTrendAnalyzer.MaxWindowDays)
            throw new ArgumentOutOfRangeException(nameof(windowDays));
    }

    private static void CleanupPreviousPerformanceTrendExports(string cacheDirectory)
    {
        try
        {
            foreach (var path in Directory.EnumerateFiles(cacheDirectory, PerformanceTrendExportPattern, SearchOption.TopDirectoryOnly))
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
                {
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException or NotSupportedException)
        {
        }
    }
}
