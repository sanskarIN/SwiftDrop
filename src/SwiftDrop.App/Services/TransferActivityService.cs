namespace SwiftDrop.App.Services;

public sealed class TransferActivityService
{
    private readonly object _gate = new();
    private int _activeTransfers;

    public Task<IAsyncDisposable> EnterAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var shouldStart = false;
        lock (_gate)
        {
            _activeTransfers++;
            shouldStart = _activeTransfers == 1;
        }
        if (shouldStart) StartPlatformActivity();
        return Task.FromResult<IAsyncDisposable>(new Lease(this));
    }

    private void Release()
    {
        var shouldStop = false;
        lock (_gate)
        {
            if (_activeTransfers <= 0) return;
            _activeTransfers--;
            shouldStop = _activeTransfers == 0;
        }
        if (shouldStop) StopPlatformActivity();
    }

    private static void StartPlatformActivity()
    {
#if ANDROID
        global::SwiftDrop.App.Platforms.Android.AndroidTransferForegroundService.Start();
#endif
    }

    private static void StopPlatformActivity()
    {
#if ANDROID
        global::SwiftDrop.App.Platforms.Android.AndroidTransferForegroundService.Stop();
#endif
    }

    private sealed class Lease : IAsyncDisposable
    {
        private TransferActivityService? _owner;

        public Lease(TransferActivityService owner) => _owner = owner;

        public ValueTask DisposeAsync()
        {
            Interlocked.Exchange(ref _owner, null)?.Release();
            return ValueTask.CompletedTask;
        }
    }
}
