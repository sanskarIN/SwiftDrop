using SwiftDrop.App.Services;

namespace SwiftDrop.App;

public partial class App : Application
{
    public App(AppSettingsService settings, MainPage page)
    {
        InitializeComponent();
        var saved = settings.Load();
        UserAppTheme = saved.Theme switch
        {
            "Light" => AppTheme.Light,
            "Dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
        MainPage = new NavigationPage(page);
    }
}
