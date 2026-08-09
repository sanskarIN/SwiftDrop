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
        builder.Services.AddSingleton<NetworkDiagnosticsService>();
        builder.Services.AddSingleton<MainPage>();
        return builder.Build();
    }
}
