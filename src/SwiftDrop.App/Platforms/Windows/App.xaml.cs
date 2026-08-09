using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using SwiftDrop.App.Services;
using Windows.ApplicationModel.Activation;

namespace SwiftDrop.App.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        InitializeComponent();
        AppInstance.GetCurrent().Activated += OnActivated;
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        HandleActivation(AppInstance.GetCurrent().GetActivatedEventArgs());
    }

    private static void OnActivated(object? sender, AppActivationArguments args)
        => HandleActivation(args);

    private static void HandleActivation(AppActivationArguments args)
    {
        if (args.Kind != ExtendedActivationKind.Protocol || args.Data is not IProtocolActivatedEventArgs protocol)
            return;
        var link = protocol.Uri?.AbsoluteUri;
        if (!string.IsNullOrWhiteSpace(link)) ExternalInputInbox.SetPairingLink(link);
    }
}
