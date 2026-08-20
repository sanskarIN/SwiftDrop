using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;
using SwiftDrop.Core.Storage;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Desktop.Services;

public sealed class DesktopBatchResumeStateService
{
    private static readonly TimeSpan Retention = TimeSpan.FromDays(7);
    private readonly BatchCompletionStore _store;
    private readonly string _databasePath;
    private readonly string _receiveRoot;
    private readonly string _receiveRootKey;
    private readonly SemaphoreSlim _initializeGate = new(1, 1);
    private bool _initialized;
    private bool _persistenceAvailable = true;

    public DesktopBatchResumeStateService(string receiveRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(receiveRoot);
        _receiveRoot = Path.GetFullPath(receiveRoot);
        _receiveRootKey = ReceiveRootKey.Create(_receiveRoot);
        DesktopPaths.EnsurePrivateDirectory(DesktopPaths.DataRoot);
        _databasePath = Path.Combine(DesktopPaths.DataRoot, "swiftdrop.db");
        _store = new BatchCompletionStore(_databasePath);
    }

    public async Task<CompletedBatchItem?> TryGetVerifiedAsync(
        string transferId,
        FileManifestEntry sourceEntry,
        CancellationToken ct = default)
    {
        if (!await EnsureInitializedAsync(ct)) return null;
        try
        {
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
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            _persistenceAvailable = false;
            return null;
        }
    }

    public async Task RecordCompletedAsync(
        string transferId,
        string sourceRelativePath,
        FileManifestEntry effectiveEntry,
        CancellationToken ct = default)
    {
        if (!await EnsureInitializedAsync(ct)) return;
        try
        {
            await _store.UpsertAsync(new CompletedBatchItem(
                transferId,
                sourceRelativePath,
                _receiveRootKey,
                effectiveEntry.RelativePath,
                effectiveEntry.Length,
                effectiveEntry.Sha256,
                DateTimeOffset.UtcNow), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            _persistenceAvailable = false;
        }
    }

    private async Task<bool> EnsureInitializedAsync(CancellationToken ct)
    {
        if (!_persistenceAvailable) return false;
        if (_initialized) return true;
        await _initializeGate.WaitAsync(ct);
        try
        {
            if (!_persistenceAvailable) return false;
            if (_initialized) return true;
            try
            {
                await _store.InitializeAsync(ct);
                DesktopPaths.RestrictPrivateFile(_databasePath);
                await _store.PruneAsync(DateTimeOffset.UtcNow - Retention, ct: ct);
                _initialized = true;
                return true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                _persistenceAvailable = false;
                return false;
            }
        }
        finally
        {
            _initializeGate.Release();
        }
    }
}
