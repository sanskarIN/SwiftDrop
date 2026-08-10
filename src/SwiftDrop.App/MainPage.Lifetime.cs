namespace SwiftDrop.App;

public partial class MainPage : IAsyncDisposable
{
    private int _lifetimeDisposed;

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _lifetimeDisposed, 1) != 0) return;

        _settings.Changed -= SettingsChanged;
        _singleCts?.Cancel();
        _batchCts?.Cancel();
        DisposePlatformIntegrations();

        await StopReceiveServerAsync();

        _singleCts?.Dispose();
        _singleCts = null;
        _batchCts?.Dispose();
        _batchCts = null;
    }

    partial void DisposePlatformIntegrations();
}
