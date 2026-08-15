#if IOS || MACCATALYST
using UserNotifications;
#endif
#if WINDOWS
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
#endif

namespace SwiftDrop.App.Services;

public sealed class TransferNotificationService : IDisposable
{
    private readonly AppSettingsService _settings;
#if IOS || MACCATALYST
    private readonly TransferNotificationCenterDelegate _appleDelegate;
#endif
#if WINDOWS
    private readonly object _windowsSync = new();
    private AppNotificationManager? _windowsManager;
    private bool _windowsRegistered;
#endif

    public TransferNotificationService(AppSettingsService settings)
    {
        _settings = settings;
#if IOS || MACCATALYST
        _appleDelegate = new TransferNotificationCenterDelegate();
        UNUserNotificationCenter.Current.Delegate = _appleDelegate;
#endif
    }

    public bool IsSupported
    {
        get
        {
#if ANDROID || IOS || MACCATALYST || WINDOWS
            return true;
#else
            return false;
#endif
        }
    }

    public async Task<bool> EnsurePermissionAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
#if ANDROID
        if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Tiramisu)
            return true;

        var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        if (status == PermissionStatus.Granted) return true;
        status = await Permissions.RequestAsync<Permissions.PostNotifications>();
        return status == PermissionStatus.Granted;
#elif IOS || MACCATALYST
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = ct.Register(() => tcs.TrySetCanceled(ct));
        UNUserNotificationCenter.Current.RequestAuthorization(
            UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound,
            (approved, error) => tcs.TrySetResult(approved && error is null));
        return await tcs.Task;
#elif WINDOWS
        await Task.CompletedTask;
        return EnsureWindowsRegistered();
#else
        await Task.CompletedTask;
        return false;
#endif
    }

    public Task NotifyCompletedAsync(CancellationToken ct = default)
        => NotifyAsync(success: true, ct);

    public Task NotifyFailedAsync(CancellationToken ct = default)
        => NotifyAsync(success: false, ct);

    private Task NotifyAsync(bool success, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!_settings.Load().NotificationsEnabled) return Task.CompletedTask;

#if ANDROID
        Platforms.Android.AndroidTransferForegroundService.ShowTerminalNotification(success);
        return Task.CompletedTask;
#elif IOS || MACCATALYST
        return ShowAppleNotificationAsync(success, ct);
#elif WINDOWS
        ShowWindowsNotification(success);
        return Task.CompletedTask;
#else
        return Task.CompletedTask;
#endif
    }

#if IOS || MACCATALYST
    private static async Task ShowAppleNotificationAsync(bool success, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var content = new UNMutableNotificationContent
        {
            Title = AppText.AppName,
            Body = AppText.Get(success ? "TransferCompletedNotification" : "TransferFailedNotification"),
            Sound = UNNotificationSound.Default
        };
        using var request = UNNotificationRequest.FromIdentifier(
            $"swiftdrop-transfer-{Guid.NewGuid():N}",
            content,
            trigger: null);
        await UNUserNotificationCenter.Current.AddNotificationRequestAsync(request);
        ct.ThrowIfCancellationRequested();
    }

    private sealed class TransferNotificationCenterDelegate : UNUserNotificationCenterDelegate
    {
        public override void WillPresentNotification(
            UNUserNotificationCenter center,
            UNNotification notification,
            Action<UNNotificationPresentationOptions> completionHandler)
        {
            completionHandler(UNNotificationPresentationOptions.Banner | UNNotificationPresentationOptions.Sound);
        }
    }
#endif

#if WINDOWS
    private bool EnsureWindowsRegistered()
    {
        lock (_windowsSync)
        {
            if (_windowsRegistered) return true;
            try
            {
                var manager = AppNotificationManager.Default;
                manager.NotificationInvoked += OnWindowsNotificationInvoked;
                try
                {
                    manager.Register();
                }
                catch
                {
                    manager.NotificationInvoked -= OnWindowsNotificationInvoked;
                    throw;
                }

                _windowsManager = manager;
                _windowsRegistered = true;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    private void ShowWindowsNotification(bool success)
    {
        if (!EnsureWindowsRegistered()) return;
        var notification = new AppNotificationBuilder()
            .AddText(AppText.AppName)
            .AddText(AppText.Get(success ? "TransferCompletedNotification" : "TransferFailedNotification"))
            .BuildNotification();
        _windowsManager!.Show(notification);
    }

    private static void OnWindowsNotificationInvoked(
        AppNotificationManager sender,
        AppNotificationActivatedEventArgs args)
    {
        // Terminal notifications are intentionally informational and contain no transfer identifiers.
        // Registration still prevents duplicate activation processes while SwiftDrop is already running.
    }
#endif

    public void Dispose()
    {
#if IOS || MACCATALYST
        if (ReferenceEquals(UNUserNotificationCenter.Current.Delegate, _appleDelegate))
            UNUserNotificationCenter.Current.Delegate = null;
        _appleDelegate.Dispose();
#endif
#if WINDOWS
        lock (_windowsSync)
        {
            if (!_windowsRegistered || _windowsManager is null) return;
            try
            {
                _windowsManager.NotificationInvoked -= OnWindowsNotificationInvoked;
                _windowsManager.Unregister();
            }
            catch
            {
                // Shutdown cleanup must not crash the application.
            }
            finally
            {
                _windowsRegistered = false;
                _windowsManager = null;
            }
        }
#endif
    }
}
