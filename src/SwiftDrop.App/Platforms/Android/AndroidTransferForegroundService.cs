using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;

namespace SwiftDrop.App.Platforms.Android;

[Service(
    Exported = false,
    ForegroundServiceType = ForegroundService.TypeDataSync)]
public sealed class AndroidTransferForegroundService : Service
{
    private const string ChannelId = "swiftdrop_transfers";
    private const int ForegroundNotificationId = 47821;
    private const int TerminalNotificationId = 47822;

    public override IBinder? OnBind(Intent? intent) => null;

    public override void OnCreate()
    {
        base.OnCreate();
        EnsureChannel(this);
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        var notification = BuildProgressNotification(this);
        StartForeground(ForegroundNotificationId, notification);
        return StartCommandResult.NotSticky;
    }

    public static void Start()
    {
        var context = global::Android.App.Application.Context;
        if (context is null) return;

        using var intent = new Intent(context, typeof(AndroidTransferForegroundService));
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
            context.StartForegroundService(intent);
        else
            context.StartService(intent);
    }

    public static void Stop()
    {
        var context = global::Android.App.Application.Context;
        if (context is null) return;

        using var intent = new Intent(context, typeof(AndroidTransferForegroundService));
        context.StopService(intent);
    }

    public static void ShowTerminalNotification(bool success)
    {
        try
        {
            var context = global::Android.App.Application.Context;
            if (context is null) return;

            EnsureChannel(context);
            using var builder = new NotificationCompat.Builder(context, ChannelId);
            _ = builder.SetContentTitle(success ? "SwiftDrop transfer completed" : "SwiftDrop transfer failed");
            _ = builder.SetContentText(success
                ? "A local SwiftDrop transfer completed."
                : "A local SwiftDrop transfer did not complete.");
            _ = builder.SetSmallIcon(Resource.Mipmap.appicon);
            _ = builder.SetAutoCancel(true);
            _ = builder.SetCategory(NotificationCompat.CategoryStatus);
            _ = builder.SetPriority((int)NotificationPriority.Low);

            var pending = CreateLaunchPendingIntent(context);
            if (pending is not null) _ = builder.SetContentIntent(pending);

            using var notification = builder.Build();
            if (notification is null) return;
            var manager = NotificationManagerCompat.From(context);
            manager?.Notify(TerminalNotificationId, notification);
        }
        catch
        {
            // Notification permission/platform policy must never make a transfer fail.
        }
    }

    private static Notification BuildProgressNotification(Context context)
    {
        using var builder = new NotificationCompat.Builder(context, ChannelId);
        _ = builder.SetContentTitle("SwiftDrop transfer in progress");
        _ = builder.SetContentText("Keep SwiftDrop available while the local transfer is active.");
        _ = builder.SetSmallIcon(Resource.Mipmap.appicon);
        _ = builder.SetOngoing(true);
        _ = builder.SetOnlyAlertOnce(true);
        _ = builder.SetCategory(NotificationCompat.CategoryProgress);
        _ = builder.SetPriority((int)NotificationPriority.Low);

        var pending = CreateLaunchPendingIntent(context);
        if (pending is not null) _ = builder.SetContentIntent(pending);

        return builder.Build()
            ?? throw new InvalidOperationException("Android notification builder returned no progress notification.");
    }

    private static PendingIntent? CreateLaunchPendingIntent(Context context)
    {
        var packageName = context.PackageName;
        if (string.IsNullOrWhiteSpace(packageName)) return null;

        var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(packageName);
        if (launchIntent is null) return null;

        var flags = PendingIntentFlags.UpdateCurrent;
        if (OperatingSystem.IsAndroidVersionAtLeast(23)) flags |= PendingIntentFlags.Immutable;
        return PendingIntent.GetActivity(context, 0, launchIntent, flags);
    }

    private static void EnsureChannel(Context context)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;

        var manager = context.GetSystemService(NotificationService) as NotificationManager;
        if (manager is null) return;

        using var channel = new NotificationChannel(
            ChannelId,
            "SwiftDrop transfers",
            NotificationImportance.Low)
        {
            Description = "Privacy-safe status notifications for user-initiated local SwiftDrop transfers."
        };
        manager.CreateNotificationChannel(channel);
    }
}
