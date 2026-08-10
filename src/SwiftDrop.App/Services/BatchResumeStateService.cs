using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;
using SwiftDrop.Core.Storage;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.App.Services;

public sealed class BatchResumeStateService
{
    private static readonly TimeSpan Retention = TimeSpan.FromDays(7);
    private readonly BatchCompletionStore _store;
    private readonly string _receiveRoot;
    private readonly string _receiveRootKey;
    private readonly SemaphoreSlim _initializeGate = new(1, 1);
    private bool _initialized;

    public BatchResumeStateService(string receiveRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(receiveRoot);
        _receiveRoot = Path.GetFullPath(receiveRoot);
        _receiveRootKey = ReceiveRootKey.Create(_receiveRoot);
        _store = new BatchCompletionStore(Path.Combine(FileSystem.AppDataDirectory, "swiftdrop.db"));
    }

    public async Task<CompletedBatchItem?> TryGetVerifiedAsync(
        string transferId,
        FileManifestEntry sourceEntry,
        CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);
        var completion = await _store.GetAsync(transferId, sourceEntry.RelativePath, _receiveRootKey, ct);
        if (completion is null) return null;

        var verifiedDestination = await BatchCompletionVerifier.TryVerifyAsync(
            _receiveRoot,
            completion,
            sourceEntry,
            ct);
        if (verifiedDestination is not null) return completion;

        await _store.RemoveAsync(transferId, sourceEntry.RelativePath, _receiveRootKey, ct);
        return null;
    }

    public async Task RecordCompletedAsync(
        string transferId,
        string sourceRelativePath,
        FileManifestEntry effectiveEntry,
        CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);
        var item = new CompletedBatchItem(
            transferId,
            sourceRelativePath,
            _receiveRootKey,
            effectiveEntry.RelativePath,
            effectiveEntry.Length,
            effectiveEntry.Sha256,
            DateTimeOffset.UtcNow);
        await _store.UpsertAsync(item, ct);
    }

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_initialized) return;
        await _initializeGate.WaitAsync(ct);
        try
        {
            if (_initialized) return;
            await _store.InitializeAsync(ct);
            await _store.PruneAsync(DateTimeOffset.UtcNow - Retention, ct: ct);
            _initialized = true;
        }
        finally
        {
            _initializeGate.Release();
        }
    }
}
