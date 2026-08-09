using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.App.Services;
using SwiftDrop.Core.Models;

namespace SwiftDrop.App;

public partial class SettingsPage : ContentPage
{
    private readonly AppSettingsService _settings;
    private readonly TransferHistoryService _history;
    private readonly IServiceProvider _services;

    public SettingsPage(AppSettingsService settings, TransferHistoryService history, IServiceProvider services)
    {
        InitializeComponent();
        _settings = settings;
        _history = history;
        _services = services;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = _settings.Load();
        ConcurrencyStepper.Value = settings.TransferConcurrency;
        RetentionStepper.Value = settings.HistoryRetentionDays;
        PrivacyModeSwitch.IsToggled = settings.PrivacyMode;
        AutoAcceptSwitch.IsToggled = settings.AutoAcceptTrustedDevices;
        ThemePicker.SelectedItem = settings.Theme;
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

    private async void ManageTrustedDevicesClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(_services.GetRequiredService<TrustedDevicesPage>());

    private async void SaveClicked(object? sender, EventArgs e)
    {
        try
        {
            var settings = new AppSettings(
                (int)ConcurrencyStepper.Value,
                (int)RetentionStepper.Value,
                PrivacyModeSwitch.IsToggled,
                AutoAcceptSwitch.IsToggled,
                ThemePicker.SelectedItem?.ToString() ?? "System");
            _settings.Save(settings);
            await _history.ApplyRetentionAsync();
            Application.Current!.UserAppTheme = settings.Theme switch
            {
                "Light" => AppTheme.Light,
                "Dark" => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
            await DisplayAlert("Saved", "Settings were saved on this device.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Settings error", ex.Message, "OK");
        }
    }

    private async void ResetClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlert("Reset settings", "Restore SwiftDrop settings to their defaults?", "Reset", "Cancel");
        if (!confirm) return;
        _settings.Reset();
        await _history.ApplyRetentionAsync();
        Application.Current!.UserAppTheme = AppTheme.Unspecified;
        LoadSettings();
    }
}
