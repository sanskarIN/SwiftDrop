using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.App.Services;
using SwiftDrop.App.ViewModels;

namespace SwiftDrop.App;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;
    private readonly IServiceProvider _services;

    public SettingsPage(SettingsViewModel viewModel, IServiceProvider services)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _services = services;
        BindingContext = viewModel;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            await _viewModel.LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Settings error", ex.Message, "OK");
        }
    }

    private void NumericSettingChanged(object? sender, ValueChangedEventArgs e)
        => _viewModel.UpdateComputedLabels();

    private async void ChooseReceiveFolderClicked(object? sender, EventArgs e)
    {
        try
        {
            var selected = await _viewModel.ChooseReceiveFolderAsync();
            if (selected) return;
#if WINDOWS
            return;
#else
            await DisplayAlertAsync(
                "Folder picker unavailable",
                "SwiftDrop keeps received files in its app-private Received folder on this platform instead of asking for broad storage access.",
                "OK");
#endif
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Folder selection failed", ex.Message, "OK");
        }
    }

    private void UseAppReceiveFolderClicked(object? sender, EventArgs e)
        => _viewModel.UseAppReceiveFolder();

    private async void ManageTrustedDevicesClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(_services.GetRequiredService<TrustedDevicesPage>());

    private async void OpenAboutClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(_services.GetRequiredService<AboutPage>());

    private async void OpenDiagnosticsClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(_services.GetRequiredService<DiagnosticsPage>());

    private async void SaveClicked(object? sender, EventArgs e)
    {
        try
        {
            var result = await _viewModel.SaveAsync();
            if (result.NotificationPermissionDenied)
            {
                await DisplayAlertAsync(
                    "Notification permission not granted",
                    "SwiftDrop will continue transferring normally without optional completion/failure notifications. The required foreground transfer status is controlled by Android platform rules.",
                    "OK");
            }

            var message = result.LanguageChanged
                ? "Settings and device name were saved. Newly opened screens use the selected language; restart SwiftDrop to refresh screens that were already open."
                : "Settings and device name were saved on this device.";
            await DisplayAlertAsync("Saved", message, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Settings error", ex.Message, "OK");
        }
    }

    private async void ResetIdentityClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync(
            "Reset device identity?",
            "This creates a new device ID and certificate, invalidates current pairing invitations, and removes every locally trusted device. Other devices will no longer recognize this identity. Received files and transfer history are not deleted.",
            "Reset identity",
            AppText.Get("Cancel"));
        if (!confirm) return;

        await _viewModel.ResetIdentityAsync();
        await DisplayAlertAsync("Identity reset", "A new local identity and certificate were created.", "OK");
    }

    private async void ResetClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync(
            "Reset settings",
            "Restore SwiftDrop settings to their defaults? Device identity and trusted devices are not changed.",
            "Reset",
            AppText.Get("Cancel"));
        if (!confirm) return;
        await _viewModel.ResetSettingsAsync();
    }
}
