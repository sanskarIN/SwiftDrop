namespace SwiftDrop.App.Services;

public sealed class PlatformMulticastLease : IDisposable
{
#if ANDROID
    private readonly Android.Net.Wifi.WifiManager.MulticastLock? _lock;
#endif
    private bool _disposed;

    private PlatformMulticastLease()
    {
#if ANDROID
        var context = Android.App.Application.Context;
        var wifi = context.GetSystemService(Android.Content.Context.WifiService) as Android.Net.Wifi.WifiManager;
        _lock = wifi?.CreateMulticastLock("SwiftDrop.mDNS");
        if (_lock is not null)
        {
            _lock.SetReferenceCounted(false);
            _lock.Acquire();
        }
#endif
    }

    public static PlatformMulticastLease Acquire() => new();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
#if ANDROID
        if (_lock?.IsHeld == true) _lock.Release();
        _lock?.Dispose();
#endif
    }
}
