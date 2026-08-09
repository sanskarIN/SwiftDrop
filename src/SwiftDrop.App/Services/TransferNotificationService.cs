namespace SwiftDrop.App.Services;

public sealed class TransferNotificationService
{
    private readonly AppSettingsService _settings;

    public TransferNotificationService(AppSettingsService settings)
    {
        _settings = settings;
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
#endif
        return Task.CompletedTask;
    }
}
