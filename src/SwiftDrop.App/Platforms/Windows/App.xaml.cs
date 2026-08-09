using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using SwiftDrop.App.Services;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer;

namespace SwiftDrop.App.WinUI;

public partial class App : MauiWinUIApplication
{
    private UIElement? _dropSurface;

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
        MainThread.BeginInvokeOnMainThread(TryEnableDesktopDrop);
    }

    private void OnActivated(object? sender, AppActivationArguments args)
    {
        HandleActivation(args);
        MainThread.BeginInvokeOnMainThread(TryEnableDesktopDrop);
    }

    private static void HandleActivation(AppActivationArguments args)
    {
        if (args.Kind != ExtendedActivationKind.Protocol || args.Data is not IProtocolActivatedEventArgs protocol)
            return;
        var link = protocol.Uri?.AbsoluteUri;
        if (!string.IsNullOrWhiteSpace(link)) ExternalInputInbox.SetPairingLink(link);
    }

    private void TryEnableDesktopDrop()
    {
        var window = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (window?.Content is not UIElement surface || ReferenceEquals(surface, _dropSurface)) return;

        DisableDesktopDrop();
        _dropSurface = surface;
        _dropSurface.AllowDrop = true;
        _dropSurface.DragOver += OnDragOver;
        _dropSurface.Drop += OnDrop;
    }

    private void DisableDesktopDrop()
    {
        if (_dropSurface is null) return;
        _dropSurface.DragOver -= OnDragOver;
        _dropSurface.Drop -= OnDrop;
        _dropSurface = null;
    }

    private static void OnDragOver(object sender, DragEventArgs e)
    {
        var data = e.DataView;
        if (data.Contains(StandardDataFormats.StorageItems) || data.Contains(StandardDataFormats.Text))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.Handled = true;
        }
    }

    private static async void OnDrop(object sender, DragEventArgs e)
    {
        try
        {
            var data = e.DataView;
            if (data.Contains(StandardDataFormats.StorageItems))
            {
                var items = await data.GetStorageItemsAsync();
                foreach (var item in items.Take(2048))
                {
                    if (!string.IsNullOrWhiteSpace(item.Path))
                        ExternalInputInbox.AddSharedPath(item.Path);
                }
            }

            if (data.Contains(StandardDataFormats.Text))
            {
                var text = await data.GetTextAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    if (text.TrimStart().StartsWith("swiftdrop://pair", StringComparison.OrdinalIgnoreCase))
                        ExternalInputInbox.SetPairingLink(text.Trim());
                    else
                        ExternalInputInbox.SetSharedText(text);
                }
            }

            e.AcceptedOperation = DataPackageOperation.Copy;
            e.Handled = true;
        }
        catch
        {
            e.AcceptedOperation = DataPackageOperation.None;
            e.Handled = true;
        }
    }
}
