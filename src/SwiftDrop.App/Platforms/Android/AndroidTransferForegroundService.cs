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
        StartForeground(ForegroundNotificationId, BuildProgressNotification(this));
        return StartCommandResult.NotSticky;
    }

    public static void Start()
    {
        var context = Application.Context;
        var intent = new Intent(context, typeof(AndroidTransferForegroundService));
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            context.StartForegroundService(intent);
        else
            context.StartService(intent);
    }

    public static void Stop()
    {
        var context = Application.Context;
        context.StopService(new Intent(context, typeof(AndroidTransferForegroundService)));
    }

    public static void ShowTerminalNotification(bool success)
    {
        try
        {
            var context = Application.Context;
            EnsureChannel(context);
            var builder = new NotificationCompat.Builder(context, ChannelId)
                .SetContentTitle(success ? "SwiftDrop transfer completed" : "SwiftDrop transfer failed")
                .SetContentText(success
                    ? "A local SwiftDrop transfer completed."
                    : "A local SwiftDrop transfer did not complete.")
                .SetSmallIcon(Resource.Mipmap.appicon)
                .SetAutoCancel(true)
                .SetCategory(NotificationCompat.CategoryStatus)
                .SetPriority((int)NotificationPriority.Low);

            var pending = CreateLaunchPendingIntent(context);
            if (pending is not null) builder.SetContentIntent(pending);
            NotificationManagerCompat.From(context).Notify(TerminalNotificationId, builder.Build());
        }
        catch
        {
            // Notification permission/platform policy must never make a transfer fail.
        }
    }

    private static Notification BuildProgressNotification(Context context)
    {
        var builder = new NotificationCompat.Builder(context, ChannelId)
            .SetContentTitle("SwiftDrop transfer in progress")
            .SetContentText("Keep SwiftDrop available while the local transfer is active.")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetOngoing(true)
            .SetOnlyAlertOnce(true)
            .SetCategory(NotificationCompat.CategoryProgress)
            .SetPriority((int)NotificationPriority.Low);
        var pending = CreateLaunchPendingIntent(context);
        if (pending is not null) builder.SetContentIntent(pending);
        return builder.Build();
    }

    private static PendingIntent? CreateLaunchPendingIntent(Context context)
    {
        var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
        if (launchIntent is null) return null;
        var flags = PendingIntentFlags.UpdateCurrent;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M) flags |= PendingIntentFlags.Immutable;
        return PendingIntent.GetActivity(context, 0, launchIntent, flags);
    }

    private static void EnsureChannel(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
        var manager = (NotificationManager?)context.GetSystemService(NotificationService);
        if (manager is null) return;
        var channel = new NotificationChannel(
            ChannelId,
            "SwiftDrop transfers",
            NotificationImportance.Low)
        {
            Description = "Privacy-safe status notifications for user-initiated local SwiftDrop transfers."
        };
        manager.CreateNotificationChannel(channel);
    }
}
