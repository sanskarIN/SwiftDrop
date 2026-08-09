namespace SwiftDrop.App.Services;

public sealed class PlatformMulticastLease : IDisposable
{
    private int _disposed;

    private PlatformMulticastLease()
    {
#if ANDROID
        Platforms.Android.AndroidMulticastLockManager.Acquire();
#endif
    }

    public static PlatformMulticastLease Acquire() => new();

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
#if ANDROID
        Platforms.Android.AndroidMulticastLockManager.Release();
#endif
    }
}
