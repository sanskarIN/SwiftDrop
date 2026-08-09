using SwiftDrop.Core.Models;
using SwiftDrop.Core.Storage;

namespace SwiftDrop.App.Services;

public sealed class TransferHistoryService
{
    private readonly TransferHistoryStore _store;
    private readonly AppSettingsService _settings;

    public TransferHistoryService(AppSettingsService settings)
    {
        _settings = settings;
        _store = new TransferHistoryStore(Path.Combine(FileSystem.AppDataDirectory, "swiftdrop.db"));
    }

    public Task InitializeAsync(CancellationToken ct = default) => _store.InitializeAsync(ct);

    public Task AddAsync(
        string direction,
        string peerDeviceName,
        string fileName,
        long sizeBytes,
        string status,
        bool integrityVerified,
        CancellationToken ct = default)
    {
        var settings = _settings.Load();
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
        return _store.AddAsync(entry, ct);
    }

    public Task<IReadOnlyList<TransferHistoryEntry>> GetRecentAsync(int limit = 100, CancellationToken ct = default)
        => _store.GetRecentAsync(limit, ct);

    public Task ClearAsync(CancellationToken ct = default) => _store.ClearAsync(ct);
}
