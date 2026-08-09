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
    private const int NotificationId = 47821;

    public override IBinder? OnBind(Intent? intent) => null;

    public override void OnCreate()
    {
        base.OnCreate();
        EnsureChannel();
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        StartForeground(NotificationId, BuildNotification());
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

    private Notification BuildNotification()
    {
        var launchIntent = PackageManager?.GetLaunchIntentForPackage(PackageName!);
        PendingIntent? pending = null;
        if (launchIntent is not null)
        {
            var flags = PendingIntentFlags.UpdateCurrent;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M) flags |= PendingIntentFlags.Immutable;
            pending = PendingIntent.GetActivity(this, 0, launchIntent, flags);
        }

        var builder = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("SwiftDrop transfer in progress")
            .SetContentText("Keep SwiftDrop available while the local transfer is active.")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetOngoing(true)
            .SetOnlyAlertOnce(true)
            .SetCategory(NotificationCompat.CategoryProgress)
            .SetPriority((int)NotificationPriority.Low);
        if (pending is not null) builder.SetContentIntent(pending);
        return builder.Build();
    }

    private void EnsureChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
        var manager = (NotificationManager?)GetSystemService(NotificationService);
        if (manager is null) return;
        var channel = new NotificationChannel(
            ChannelId,
            "SwiftDrop transfers",
            NotificationImportance.Low)
        {
            Description = "Status notification shown while user-initiated local transfers are active."
        };
        manager.CreateNotificationChannel(channel);
    }
}
