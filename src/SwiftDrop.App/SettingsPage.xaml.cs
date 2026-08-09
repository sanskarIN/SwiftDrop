using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.App.Services;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;

namespace SwiftDrop.App;

public partial class SettingsPage : ContentPage
{
    private readonly AppSettingsService _settings;
    private readonly TransferHistoryService _history;
    private readonly DeviceIdentityService _identity;
    private readonly TrustedDevicesService _trustedDevices;
    private readonly ReceiveLocationService _receiveLocation;
    private readonly AppearanceService _appearance;
    private readonly IServiceProvider _services;
    private bool _useDefaultReceiveFolder = true;

    public SettingsPage(
        AppSettingsService settings,
        TransferHistoryService history,
        DeviceIdentityService identity,
        TrustedDevicesService trustedDevices,
        ReceiveLocationService receiveLocation,
        AppearanceService appearance,
        IServiceProvider services)
    {
        InitializeComponent();
        _settings = settings;
        _history = history;
        _identity = identity;
        _trustedDevices = trustedDevices;
        _receiveLocation = receiveLocation;
        _appearance = appearance;
        _services = services;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        await _identity.InitializeAsync();
        var settings = _settings.Load();
        DeviceNameEntry.Text = _identity.DeviceName;
        IdentityFingerprintLabel.Text = $"Certificate fingerprint: {Fingerprint.Pretty(Fingerprint.FromCertificate(_identity.Certificate))}";
        ConcurrencyStepper.Value = settings.TransferConcurrency;
        RetentionStepper.Value = settings.HistoryRetentionDays;
        PrivacyModeSwitch.IsToggled = settings.PrivacyMode;
        AutoAcceptSwitch.IsToggled = settings.AutoAcceptTrustedDevices;
        NotificationsSwitch.IsToggled = settings.NotificationsEnabled;
        ReduceMotionSwitch.IsToggled = settings.ReduceMotion;
        LargerInterfaceSwitch.IsToggled = settings.LargerInterface;
        DeveloperOptionsSwitch.IsToggled = settings.DeveloperOptionsEnabled;
        ThemePicker.SelectedItem = settings.Theme;
        LanguagePicker.SelectedItem = string.Equals(settings.Language, "hi", StringComparison.OrdinalIgnoreCase) ? "Hindi" : "English";
        _useDefaultReceiveFolder = string.IsNullOrWhiteSpace(settings.DefaultReceiveFolder);
        ReceiveFolderEntry.Text = _useDefaultReceiveFolder
            ? _receiveLocation.GetDefaultAppReceiveRoot()
            : settings.DefaultReceiveFolder;
#if WINDOWS
        ReceiveFolderSupportLabel.Text = "Windows uses the system folder picker. SwiftDrop requests access only to the folder you choose.";
#else
        ReceiveFolderSupportLabel.Text = "This platform currently uses SwiftDrop's app-private Received folder. Custom external-folder selection is disabled rather than requesting broad filesystem access.";
#endif
        UpdateLabels();
    }

    private void UpdateLabels()
    {
        ConcurrencyLabel.Text = $"{(int)ConcurrencyStepper.Value}";
        RetentionLabel.Text = RetentionStepper.Value == 0
            ? "Do not retain history"
            : $"{(int)RetentionStepper.Value} days";
    }

    private void ConcurrencyChanged(object? sender, ValueChangedEventArgs e) => UpdateLabels();
    private void RetentionChanged(object? sender, ValueChangedEventArgs e) => UpdateLabels();

    private async void ChooseReceiveFolderClicked(object? sender, EventArgs e)
    {
        try
        {
            var selected = await _receiveLocation.PickFolderAsync();
            if (string.IsNullOrWhiteSpace(selected))
            {
#if WINDOWS
                return;
#else
                await DisplayAlert(
                    "Folder picker unavailable",
                    "SwiftDrop keeps received files in its app-private Received folder on this platform instead of asking for broad storage access.",
                    "OK");
                return;
#endif
            }
            _useDefaultReceiveFolder = false;
            ReceiveFolderEntry.Text = selected;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Folder selection failed", ex.Message, "OK");
        }
    }

    private void UseAppReceiveFolderClicked(object? sender, EventArgs e)
    {
        _useDefaultReceiveFolder = true;
        ReceiveFolderEntry.Text = _receiveLocation.GetDefaultAppReceiveRoot();
    }

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
            await _identity.RenameAsync(DeviceNameEntry.Text ?? string.Empty);
            var settings = new AppSettings(
                (int)ConcurrencyStepper.Value,
                (int)RetentionStepper.Value,
                PrivacyModeSwitch.IsToggled,
                AutoAcceptSwitch.IsToggled,
                ThemePicker.SelectedItem?.ToString() ?? "System",
                NotificationsSwitch.IsToggled,
                ReduceMotionSwitch.IsToggled,
                _useDefaultReceiveFolder ? string.Empty : ReceiveFolderEntry.Text ?? string.Empty,
                LargerInterfaceSwitch.IsToggled,
                string.Equals(LanguagePicker.SelectedItem?.ToString(), "Hindi", StringComparison.Ordinal) ? "hi" : "en",
                DeveloperOptionsSwitch.IsToggled);
            _settings.Save(settings);
            await _history.ApplyRetentionAsync();
            _appearance.Apply(settings);
            DeviceNameEntry.Text = _identity.DeviceName;
            await DisplayAlert("Saved", "Settings and device name were saved on this device.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Settings error", ex.Message, "OK");
        }
    }

    private async void ResetIdentityClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlert(
            "Reset device identity?",
            "This creates a new device ID and certificate, invalidates current pairing invitations, and removes every locally trusted device. Other devices will no longer recognize this identity. Received files and transfer history are not deleted.",
            "Reset identity",
            "Cancel");
        if (!confirm) return;

        await _trustedDevices.ClearAsync();
        await _identity.ResetIdentityAsync();
        DeviceNameEntry.Text = _identity.DeviceName;
        IdentityFingerprintLabel.Text = $"Certificate fingerprint: {Fingerprint.Pretty(Fingerprint.FromCertificate(_identity.Certificate))}";
        await DisplayAlert("Identity reset", "A new local identity and certificate were created.", "OK");
    }

    private async void ResetClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlert("Reset settings", "Restore SwiftDrop settings to their defaults? Device identity and trusted devices are not changed.", "Reset", "Cancel");
        if (!confirm) return;
        _settings.Reset();
        await _history.ApplyRetentionAsync();
        _appearance.Apply(AppSettings.Default);
        await LoadAsync();
    }
}
