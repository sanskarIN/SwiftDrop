using SwiftDrop.App.Services;

namespace SwiftDrop.App;

public partial class App : Application
{
    private readonly MainPage _mainPage;
    private readonly EventHandler _externalInputChanged;

    public App(AppSettingsService settings, AppearanceService appearance, MainPage page)
    {
        InitializeComponent();
        appearance.Apply(settings.Load());
        ExternalInputInbox.PruneStagedCache(TimeSpan.FromHours(24));
        _mainPage = page;
        _externalInputChanged = OnExternalInputChanged;
        ExternalInputInbox.Changed += _externalInputChanged;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new NavigationPage(_mainPage));
        window.Destroying += OnWindowDestroying;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await _mainPage.ShowIdentityRecoveryNoticeAsync();
            await _mainPage.ApplyExternalInputAsync();
        });
        return window;
    }

    private async void OnExternalInputChanged(object? sender, EventArgs e)
    {
        try
        {
            await _mainPage.ApplyExternalInputAsync();
        }
        catch
        {
        }
    }

    private async void OnWindowDestroying(object? sender, EventArgs e)
    {
        ExternalInputInbox.Changed -= _externalInputChanged;
        if (sender is Window window) window.Destroying -= OnWindowDestroying;
        await _mainPage.DisposeAsync();
    }
}
