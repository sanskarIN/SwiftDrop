namespace SwiftDrop.App.Services;

public sealed class TransferNotificationService
{
    private readonly AppSettingsService _settings;

    public TransferNotificationService(AppSettingsService settings)
    {
        _settings = settings;
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
