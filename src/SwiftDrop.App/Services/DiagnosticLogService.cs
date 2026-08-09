using System.Text;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Storage;

namespace SwiftDrop.App.Services;

public sealed class DiagnosticLogService
{
    private readonly DiagnosticEventStore _store;
    private readonly AppSettingsService _settings;
    private readonly SemaphoreSlim _initializeGate = new(1, 1);
    private bool _initialized;

    public DiagnosticLogService(AppSettingsService settings)
    {
        _settings = settings;
        _store = new DiagnosticEventStore(Path.Combine(FileSystem.AppDataDirectory, "swiftdrop.db"));
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_initialized) return;
        await _initializeGate.WaitAsync(ct);
        try
        {
            if (_initialized) return;
            await _store.InitializeAsync(ct);
            await _store.PruneAsync(DateTimeOffset.UtcNow.AddDays(-14), 1000, ct);
            _initialized = true;
        }
        finally
        {
            _initializeGate.Release();
        }
    }

    public async Task RecordAsync(
        string level,
        string code,
        string safeMessage,
        CancellationToken ct = default)
    {
        await InitializeAsync(ct);
        var settings = _settings.Load();
        var message = settings.PrivacyMode ? RedactPotentialIdentifiers(safeMessage) : safeMessage;
        var entry = new DiagnosticEvent(
            Guid.NewGuid().ToString("N"),
            DateTimeOffset.UtcNow,
            NormalizeLevel(level),
            NormalizeCode(code),
            NormalizeMessage(message));
        await _store.AddAsync(entry, ct);
    }

    public async Task<IReadOnlyList<DiagnosticEvent>> GetRecentAsync(int limit = 200, CancellationToken ct = default)
    {
        await InitializeAsync(ct);
        return await _store.GetRecentAsync(limit, ct);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        await InitializeAsync(ct);
        await _store.ClearAsync(ct);
    }

    public async Task<string> ExportSafeTextAsync(CancellationToken ct = default)
    {
        var events = await GetRecentAsync(500, ct);
        var path = Path.Combine(FileSystem.CacheDirectory, $"swiftdrop-diagnostics-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.txt");
        var builder = new StringBuilder();
        builder.AppendLine("SwiftDrop diagnostics export");
        builder.AppendLine("Contains diagnostic metadata only. It intentionally excludes transfer contents, private keys, pairing nonces, and full pairing invitations.");
        builder.AppendLine($"Generated UTC: {DateTimeOffset.UtcNow:O}");
        builder.AppendLine();
        foreach (var entry in events.OrderBy(x => x.TimestampUtc))
            builder.AppendLine($"{entry.TimestampUtc:O}\t{entry.Level}\t{entry.Code}\t{entry.Message}");
        await File.WriteAllTextAsync(path, builder.ToString(), Encoding.UTF8, ct);
        return path;
    }

    private static string NormalizeLevel(string value)
    {
        var normalized = value.Trim();
        return normalized is "Trace" or "Debug" or "Info" or "Warning" or "Error"
            ? normalized
            : "Info";
    }

    private static string NormalizeCode(string value)
    {
        var filtered = new string(value.Trim().Where(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-').Take(96).ToArray());
        return string.IsNullOrWhiteSpace(filtered) ? "unknown" : filtered;
    }

    private static string NormalizeMessage(string value)
    {
        var singleLine = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return singleLine.Length <= 512 ? singleLine : singleLine[..512];
    }

    private static string RedactPotentialIdentifiers(string value)
    {
        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(' ', words.Select(word =>
            word.Contains('@', StringComparison.Ordinal) || word.Contains('\\', StringComparison.Ordinal) || word.Contains('/', StringComparison.Ordinal)
                ? "[redacted]"
                : word));
    }
}
