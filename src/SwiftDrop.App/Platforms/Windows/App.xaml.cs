using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using SwiftDrop.App.Services;
using SwiftDrop.Core.Protocol;
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

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
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

    private static void OnDragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        var data = e.DataView;
        if (data.Contains(StandardDataFormats.StorageItems) || data.Contains(StandardDataFormats.Text))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.Handled = true;
        }
    }

    private static async void OnDrop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        try
        {
            var data = e.DataView;
            var paths = new List<string>();
            if (data.Contains(StandardDataFormats.StorageItems))
            {
                var items = await data.GetStorageItemsAsync();
                foreach (var item in items.Take(ProtocolConstants.MaxBatchFiles))
                {
                    if (!string.IsNullOrWhiteSpace(item.Path)) paths.Add(item.Path);
                }
            }

            string? sharedText = null;
            string? pairingLink = null;
            if (data.Contains(StandardDataFormats.Text))
            {
                var text = await data.GetTextAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var trimmed = text.Trim();
                    if (trimmed.StartsWith("swiftdrop://pair", StringComparison.OrdinalIgnoreCase) && trimmed.Length <= 16_384)
                        pairingLink = trimmed;
                    else
                        sharedText = text;
                }
            }

            if (pairingLink is not null) ExternalInputInbox.SetPairingLink(pairingLink);
            if (paths.Count > 0 || sharedText is not null)
                ExternalInputInbox.AddSharedBatch(sharedText, paths);

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
