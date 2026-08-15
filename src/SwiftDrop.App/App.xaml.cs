using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.App.Services;

namespace SwiftDrop.App;

public partial class App : Application
{
    private readonly MainPage _mainPage;
    private readonly TransferNotificationService _notifications;
    private readonly EventHandler _externalInputChanged;

    public App(
        AppSettingsService settings,
        AppearanceService appearance,
        TransferNotificationService notifications,
        IServiceProvider services)
    {
        InitializeComponent();
        appearance.Apply(settings.Load());
        ExternalInputInbox.PruneStagedCache(TimeSpan.FromHours(24));
        _mainPage = services.GetRequiredService<MainPage>();
        _notifications = notifications;
        _externalInputChanged = OnExternalInputChanged;
        ExternalInputInbox.Changed += _externalInputChanged;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new NavigationPage(_mainPage));
        window.Activated += OnWindowActivated;
        window.Destroying += OnWindowDestroying;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await ImportAppleSharesBestEffortAsync();
            await _mainPage.ShowIdentityRecoveryNoticeAsync();
            await _mainPage.ApplyExternalInputAsync();
        });
        return window;
    }

    private async void OnWindowActivated(object? sender, EventArgs e)
    {
        await ImportAppleSharesBestEffortAsync();
        await ApplyExternalInputBestEffortAsync();
    }

    private async void OnExternalInputChanged(object? sender, EventArgs e)
        => await ApplyExternalInputBestEffortAsync();

    private async Task ApplyExternalInputBestEffortAsync()
    {
        try
        {
            await _mainPage.ApplyExternalInputAsync();
        }
        catch
        {
        }
    }

    private static async Task ImportAppleSharesBestEffortAsync()
    {
#if IOS || MACCATALYST
        try
        {
            await AppleShareContainerImporter.ImportPendingAsync();
        }
        catch
        {
        }
#else
        await Task.CompletedTask;
#endif
    }

    private async void OnWindowDestroying(object? sender, EventArgs e)
    {
        ExternalInputInbox.Changed -= _externalInputChanged;
        if (sender is Window window)
        {
            window.Activated -= OnWindowActivated;
            window.Destroying -= OnWindowDestroying;
        }
        await _mainPage.DisposeAsync();
        _notifications.Dispose();
    }
}
