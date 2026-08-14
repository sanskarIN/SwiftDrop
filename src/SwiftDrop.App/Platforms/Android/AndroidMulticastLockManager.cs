using Android.Content;
using Android.Net.Wifi;

namespace SwiftDrop.App.Platforms.Android;

public static class AndroidMulticastLockManager
{
    private static readonly object Gate = new();
    private static WifiManager.MulticastLock? _lock;
    private static int _references;

    public static void Acquire()
    {
        lock (Gate)
        {
            _references++;
            if (_lock is not null) return;

            var context = global::Android.App.Application.Context;
            var wifi = context?.GetSystemService(Context.WifiService) as WifiManager;
            if (wifi is null)
            {
                _references--;
                return;
            }

            var multicastLock = wifi.CreateMulticastLock("SwiftDrop.mDNS");
            if (multicastLock is null)
            {
                _references--;
                return;
            }

            multicastLock.SetReferenceCounted(false);
            multicastLock.Acquire();
            _lock = multicastLock;
        }
    }

    public static void Release()
    {
        lock (Gate)
        {
            if (_references > 0) _references--;
            if (_references > 0 || _lock is null) return;
            if (_lock.IsHeld) _lock.Release();
            _lock.Dispose();
            _lock = null;
        }
    }
}
