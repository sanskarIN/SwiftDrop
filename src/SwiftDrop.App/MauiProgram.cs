using Microsoft.Extensions.Logging;
using SwiftDrop.App.Services;
using SwiftDrop.Core.Diagnostics;

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
        builder.Services.AddSingleton<TransferCoordinator>();
        builder.Services.AddSingleton<AppSettingsService>();
        builder.Services.AddSingleton<TransferHistoryService>();
        builder.Services.AddSingleton<TrustedDevicesService>();
        builder.Services.AddSingleton<NetworkDiagnosticsService>();
        builder.Services.AddSingleton<NearbyDiscoveryService>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<HistoryPage>();
        builder.Services.AddTransient<DevicesPage>();
        builder.Services.AddTransient<TrustedDevicesPage>();
        builder.Services.AddTransient<AboutPage>();
        return builder.Build();
    }
}
