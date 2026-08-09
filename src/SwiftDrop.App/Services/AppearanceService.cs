using System.Globalization;
using SwiftDrop.Core.Models;

namespace SwiftDrop.App.Services;

public sealed class AppearanceService
{
    public void Apply(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var app = Application.Current ?? throw new InvalidOperationException("Application is not initialized.");
        app.UserAppTheme = settings.Theme switch
        {
            "Light" => AppTheme.Light,
            "Dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };

        app.Resources["BodyFontSize"] = settings.LargerInterface ? 17d : 14d;
        app.Resources["ControlFontSize"] = settings.LargerInterface ? 17d : 14d;
        app.Resources["ControlMinimumHeight"] = settings.LargerInterface ? 52d : 44d;

        var cultureName = settings.Language switch
        {
            "hi" => "hi-IN",
            _ => "en"
        };
        var culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
    }
}
