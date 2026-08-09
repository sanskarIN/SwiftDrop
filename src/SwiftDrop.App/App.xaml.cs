using SwiftDrop.App.Services;

namespace SwiftDrop.App;

public partial class App : Application
{
    public App(AppSettingsService settings, AppearanceService appearance, MainPage page)
    {
        InitializeComponent();
        appearance.Apply(settings.Load());
        ExternalInputInbox.PruneStagedCache(TimeSpan.FromHours(24));
        MainPage = new NavigationPage(page);
        ExternalInputInbox.Changed += async (_, _) => await page.ApplyExternalInputAsync();
        MainThread.BeginInvokeOnMainThread(async () => await page.ApplyExternalInputAsync());
    }
}
