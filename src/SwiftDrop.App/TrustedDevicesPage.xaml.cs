using SwiftDrop.App.Services;
using SwiftDrop.App.ViewModels;

namespace SwiftDrop.App;

public partial class TrustedDevicesPage : ContentPage
{
    private readonly TrustedDevicesViewModel _viewModel;

    public TrustedDevicesPage(TrustedDevicesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        Loaded += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            await _viewModel.RefreshAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(AppText.Get("TrustedDevices"), ex.Message, AppText.Get("Ok"));
        }
    }

    private async void RefreshClicked(object? sender, EventArgs e) => await RefreshAsync();

    private async void RevokeClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not string deviceId) return;
        var confirmed = await DisplayAlertAsync(
            AppText.Get("RevokeTrustedDeviceQuestion"),
            AppText.Get("RevokeTrustedDeviceMessage"),
            AppText.Get("Revoke"),
            AppText.Get("Cancel"));
        if (!confirmed) return;
        await _viewModel.RevokeAsync(deviceId);
    }

    private async void ClearAllClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            AppText.Get("ClearTrustedDevicesQuestion"),
            AppText.Get("ClearTrustedDevicesMessage"),
            AppText.Get("ClearAll"),
            AppText.Get("Cancel"));
        if (!confirmed) return;
        await _viewModel.ClearAsync();
    }
}
