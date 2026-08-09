using SwiftDrop.App.Services;

namespace SwiftDrop.App;

public partial class App : Application
{
    public App(AppSettingsService settings, AppearanceService appearance, MainPage page)
    {
        InitializeComponent();
        appearance.Apply(settings.Load());
        MainPage = new NavigationPage(page);
    }
}
