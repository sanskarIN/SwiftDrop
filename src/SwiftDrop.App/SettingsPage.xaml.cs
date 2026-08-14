using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.App.Services;
using SwiftDrop.App.ViewModels;

namespace SwiftDrop.App;

public partial class SettingsPage : ContentPage
{
    private static readonly Uri BuyMeACoffeeUri = new("https://buymeacoffee.com/sanskarIN");
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
            await DisplayAlertAsync(AppText.Get("SettingsError"), ex.Message, AppText.Get("Ok"));
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
                AppText.Get("FolderPickerUnavailable"),
                AppText.Get("FolderPickerUnavailableMessage"),
                AppText.Get("Ok"));
#endif
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(AppText.Get("FolderSelectionFailed"), ex.Message, AppText.Get("Ok"));
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

    private async void OpenBuyMeACoffeeClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!await Launcher.Default.TryOpenAsync(BuyMeACoffeeUri))
            {
                await DisplayAlertAsync(AppText.Get("UnableToOpenLink"), AppText.Get("BuyMeACoffeeDescription"), AppText.Get("Ok"));
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
        {
            await DisplayAlertAsync(AppText.Get("UnableToOpenLink"), AppText.Get("BuyMeACoffeeDescription"), AppText.Get("Ok"));
        }
    }

    private async void SaveClicked(object? sender, EventArgs e)
    {
        try
        {
            var result = await _viewModel.SaveAsync();
            if (result.NotificationPermissionDenied)
            {
                await DisplayAlertAsync(
                    AppText.Get("NotificationPermissionNotGranted"),
                    AppText.Get("NotificationPermissionNotGrantedMessage"),
                    AppText.Get("Ok"));
            }

            var message = result.LanguageChanged
                ? AppText.Get("SavedLanguageChangedMessage")
                : AppText.Get("SavedSettingsMessage");
            await DisplayAlertAsync(AppText.Get("Saved"), message, AppText.Get("Ok"));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(AppText.Get("SettingsError"), ex.Message, AppText.Get("Ok"));
        }
    }

    private async void ResetIdentityClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync(
            AppText.Get("ResetDeviceIdentityQuestion"),
            AppText.Get("ResetDeviceIdentityQuestionMessage"),
            AppText.Get("ResetIdentityAction"),
            AppText.Get("Cancel"));
        if (!confirm) return;

        await _viewModel.ResetIdentityAsync();
        await DisplayAlertAsync(
            AppText.Get("IdentityReset"),
            AppText.Get("IdentityResetMessage"),
            AppText.Get("Ok"));
    }

    private async void ResetClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync(
            AppText.Get("ResetSettings"),
            AppText.Get("ResetSettingsQuestionMessage"),
            AppText.Get("Reset"),
            AppText.Get("Cancel"));
        if (!confirm) return;
        await _viewModel.ResetSettingsAsync();
    }
}
