using SwiftDrop.Core.Models;
using SwiftDrop.Core.Storage;

namespace SwiftDrop.App.Services;

public sealed class TransferHistoryService
{
    private const string PrivacyRedaction = "Hidden by privacy mode";
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
        CancellationToken ct = default)
    {
        await InitializeAsync(ct);
        var settings = _settings.Load();
        if (settings.HistoryRetentionDays == 0) return;
        var storedPeer = settings.PrivacyMode ? PrivacyRedaction : peerDeviceName;
        var storedName = settings.PrivacyMode ? PrivacyRedaction : fileName;
        var entry = new TransferHistoryEntry(
            Guid.NewGuid().ToString("N"),
            direction,
            storedPeer,
            storedName,
            sizeBytes,
            DateTimeOffset.UtcNow,
            status,
            integrityVerified);
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
                PeerDeviceName = PrivacyRedaction,
                FileName = PrivacyRedaction
            })
            .ToArray();
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
}
