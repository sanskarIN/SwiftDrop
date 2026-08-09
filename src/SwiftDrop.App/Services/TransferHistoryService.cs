using SwiftDrop.Core.Models;
using SwiftDrop.Core.Storage;

namespace SwiftDrop.App.Services;

public sealed class TransferHistoryService
{
    private readonly TransferHistoryStore _store;
    private readonly AppSettingsService _settings;
    private bool _initialized;

    public TransferHistoryService(AppSettingsService settings)
    {
        _settings = settings;
        _store = new TransferHistoryStore(Path.Combine(FileSystem.AppDataDirectory, "swiftdrop.db"));
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_initialized) return;
        await _store.InitializeAsync(ct);
        _initialized = true;
        await ApplyRetentionAsync(ct);
    }

    public async Task ApplyRetentionAsync(CancellationToken ct = default)
    {
        if (!_initialized) await InitializeAsync(ct);
        var days = _settings.Load().HistoryRetentionDays;
        if (days == 0)
        {
            await _store.ClearAsync(ct);
            return;
        }
        await _store.PruneBeforeAsync(DateTimeOffset.UtcNow.AddDays(-days), ct);
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
        var storedName = settings.PrivacyMode ? "Hidden by privacy mode" : fileName;
        var entry = new TransferHistoryEntry(
            Guid.NewGuid().ToString("N"),
            direction,
            peerDeviceName,
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
        return await _store.GetRecentAsync(limit, ct);
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
