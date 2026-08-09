using Microsoft.Extensions.Logging;
using SwiftDrop.App.Services;
using SwiftDrop.Core.Diagnostics;
using SwiftDrop.Core.Security;

namespace SwiftDrop.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
#if DEBUG
        builder.Logging.AddDebug();
#endif
        builder.Services.AddSingleton<DeviceIdentityService>();
        builder.Services.AddSingleton<AppSettingsService>();
        builder.Services.AddSingleton<AppearanceService>();
        builder.Services.AddSingleton<ReceiveLocationService>();
        builder.Services.AddSingleton<TransferActivityService>();
        builder.Services.AddSingleton<TransferQueueService>();
        builder.Services.AddSingleton<TransferCoordinator>();
        builder.Services.AddSingleton<TransferHistoryService>();
        builder.Services.AddSingleton<DiagnosticLogService>();
        builder.Services.AddSingleton<TrustedDevicesService>();
        builder.Services.AddSingleton<NetworkDiagnosticsService>();
        builder.Services.AddSingleton<TransferSelfTestService>();
        builder.Services.AddSingleton<NearbyDiscoveryService>();
        builder.Services.AddSingleton<NearbyPairingService>();
        builder.Services.AddSingleton<PairingSelectionService>();
        builder.Services.AddSingleton<OneTimePairingCodeManager>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<HistoryPage>();
        builder.Services.AddTransient<DevicesPage>();
        builder.Services.AddTransient<TrustedDevicesPage>();
        builder.Services.AddTransient<DiagnosticsPage>();
        builder.Services.AddTransient<AboutPage>();
        builder.Services.AddTransient<QueuePage>();
        return builder.Build();
    }
}
