using SwiftDrop.Core.Configuration;
using SwiftDrop.Core.Models;

namespace SwiftDrop.App.Services;

public sealed class AppSettingsService
{
    private const string ConcurrencyKey = "settings_transfer_concurrency";
    private const string HistoryRetentionKey = "settings_history_retention_days";
    private const string PrivacyModeKey = "settings_privacy_mode";
    private const string AutoAcceptKey = "settings_auto_accept_trusted";
    private const string ThemeKey = "settings_theme";
    private const string NotificationsKey = "settings_notifications_enabled";
    private const string ReduceMotionKey = "settings_reduce_motion";

    public AppSettings Load()
    {
        var settings = new AppSettings(
            Preferences.Default.Get(ConcurrencyKey, AppSettings.Default.TransferConcurrency),
            Preferences.Default.Get(HistoryRetentionKey, AppSettings.Default.HistoryRetentionDays),
            Preferences.Default.Get(PrivacyModeKey, AppSettings.Default.PrivacyMode),
            Preferences.Default.Get(AutoAcceptKey, AppSettings.Default.AutoAcceptTrustedDevices),
            Preferences.Default.Get(ThemeKey, AppSettings.Default.Theme),
            Preferences.Default.Get(NotificationsKey, AppSettings.Default.NotificationsEnabled),
            Preferences.Default.Get(ReduceMotionKey, AppSettings.Default.ReduceMotion));
        return SettingsValidator.Validate(settings);
    }

    public void Save(AppSettings settings)
    {
        settings = SettingsValidator.Validate(settings);
        Preferences.Default.Set(ConcurrencyKey, settings.TransferConcurrency);
        Preferences.Default.Set(HistoryRetentionKey, settings.HistoryRetentionDays);
        Preferences.Default.Set(PrivacyModeKey, settings.PrivacyMode);
        Preferences.Default.Set(AutoAcceptKey, settings.AutoAcceptTrustedDevices);
        Preferences.Default.Set(ThemeKey, settings.Theme);
        Preferences.Default.Set(NotificationsKey, settings.NotificationsEnabled);
        Preferences.Default.Set(ReduceMotionKey, settings.ReduceMotion);
    }

    public void Reset()
    {
        Save(AppSettings.Default);
    }
}
