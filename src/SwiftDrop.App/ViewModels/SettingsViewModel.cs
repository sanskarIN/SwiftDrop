using SwiftDrop.App.Services;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;

namespace SwiftDrop.App.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly AppSettingsService _settings;
    private readonly TransferHistoryService _history;
    private readonly DeviceIdentityService _identity;
    private readonly TrustedDevicesService _trustedDevices;
    private readonly ReceiveLocationService _receiveLocation;
    private readonly AppearanceService _appearance;
    private readonly TransferNotificationService _notifications;

    private string _deviceName = string.Empty;
    private string _identityFingerprint = string.Empty;
    private string _receiveFolder = string.Empty;
    private string _receiveFolderSupport = string.Empty;
    private string _notificationSupport = string.Empty;
    private double _transferConcurrency = 2;
    private double _historyRetentionDays = 30;
    private bool _privacyMode;
    private bool _autoAcceptTrustedDevices;
    private bool _notificationsEnabled;
    private bool _notificationsSupported;
    private bool _reduceMotion;
    private bool _largerInterface;
    private bool _developerOptionsEnabled;
    private int _themeIndex;
    private int _languageIndex;
    private IReadOnlyList<string> _themeOptions = Array.Empty<string>();
    private IReadOnlyList<string> _languageOptions = Array.Empty<string>();
    private bool _useDefaultReceiveFolder = true;

    public SettingsViewModel(
        AppSettingsService settings,
        TransferHistoryService history,
        DeviceIdentityService identity,
        TrustedDevicesService trustedDevices,
        ReceiveLocationService receiveLocation,
        AppearanceService appearance,
        TransferNotificationService notifications)
    {
        _settings = settings;
        _history = history;
        _identity = identity;
        _trustedDevices = trustedDevices;
        _receiveLocation = receiveLocation;
        _appearance = appearance;
        _notifications = notifications;
    }

    public string DeviceName { get => _deviceName; set => SetProperty(ref _deviceName, value); }
    public string IdentityFingerprint { get => _identityFingerprint; private set => SetProperty(ref _identityFingerprint, value); }
    public string ReceiveFolder { get => _receiveFolder; private set => SetProperty(ref _receiveFolder, value); }
    public string ReceiveFolderSupport { get => _receiveFolderSupport; private set => SetProperty(ref _receiveFolderSupport, value); }
    public string NotificationSupport { get => _notificationSupport; private set => SetProperty(ref _notificationSupport, value); }
    public double TransferConcurrency { get => _transferConcurrency; set => SetProperty(ref _transferConcurrency, value); }
    public double HistoryRetentionDays { get => _historyRetentionDays; set => SetProperty(ref _historyRetentionDays, value); }
    public bool PrivacyMode { get => _privacyMode; set => SetProperty(ref _privacyMode, value); }
    public bool AutoAcceptTrustedDevices { get => _autoAcceptTrustedDevices; set => SetProperty(ref _autoAcceptTrustedDevices, value); }
    public bool NotificationsEnabled { get => _notificationsEnabled; set => SetProperty(ref _notificationsEnabled, value); }
    public bool NotificationsSupported { get => _notificationsSupported; private set => SetProperty(ref _notificationsSupported, value); }
    public bool ReduceMotion { get => _reduceMotion; set => SetProperty(ref _reduceMotion, value); }
    public bool LargerInterface { get => _largerInterface; set => SetProperty(ref _largerInterface, value); }
    public bool DeveloperOptionsEnabled { get => _developerOptionsEnabled; set => SetProperty(ref _developerOptionsEnabled, value); }
    public int ThemeIndex { get => _themeIndex; set => SetProperty(ref _themeIndex, value); }
    public int LanguageIndex { get => _languageIndex; set => SetProperty(ref _languageIndex, value); }
    public IReadOnlyList<string> ThemeOptions { get => _themeOptions; private set => SetProperty(ref _themeOptions, value); }
    public IReadOnlyList<string> LanguageOptions { get => _languageOptions; private set => SetProperty(ref _languageOptions, value); }
    public string ConcurrencyText => $"{(int)TransferConcurrency}";
    public string RetentionText => HistoryRetentionDays == 0
        ? AppText.Get("DoNotRetainHistory")
        : AppText.Format("RetentionDaysFormat", (int)HistoryRetentionDays);

    public async Task LoadAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await _identity.InitializeAsync();
        var settings = _settings.Load();
        DeviceName = _identity.DeviceName;
        IdentityFingerprint = FormatIdentityFingerprint();
        TransferConcurrency = settings.TransferConcurrency;
        HistoryRetentionDays = settings.HistoryRetentionDays;
        PrivacyMode = settings.PrivacyMode;
        AutoAcceptTrustedDevices = settings.AutoAcceptTrustedDevices;
        NotificationsSupported = _notifications.IsSupported;
        NotificationsEnabled = NotificationsSupported && settings.NotificationsEnabled;
#if ANDROID
        NotificationSupport = AppText.Get("NotificationSupportAndroid");
#elif IOS || MACCATALYST
        NotificationSupport = AppText.Get("NotificationSupportApple");
#elif WINDOWS
        NotificationSupport = AppText.Get("NotificationSupportWindows");
#else
        NotificationSupport = AppText.Get("NotificationSupportUnavailable");
#endif
        ReduceMotion = settings.ReduceMotion;
        LargerInterface = settings.LargerInterface;
        DeveloperOptionsEnabled = settings.DeveloperOptionsEnabled;
        RefreshLocalizedOptions(settings.Theme, settings.Language);
        _useDefaultReceiveFolder = string.IsNullOrWhiteSpace(settings.DefaultReceiveFolder);
        ReceiveFolder = _useDefaultReceiveFolder
            ? _receiveLocation.GetDefaultAppReceiveRoot()
            : settings.DefaultReceiveFolder;
#if WINDOWS
        ReceiveFolderSupport = AppText.Get("ReceiveFolderSupportWindows");
#else
        ReceiveFolderSupport = AppText.Get("ReceiveFolderSupportPrivate");
#endif
        RaiseComputedLabels();
    }

    public async Task<bool> ChooseReceiveFolderAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var selected = await _receiveLocation.PickFolderAsync();
        if (string.IsNullOrWhiteSpace(selected)) return false;
        _useDefaultReceiveFolder = false;
        ReceiveFolder = selected;
        return true;
    }

    public void UseAppReceiveFolder()
    {
        _useDefaultReceiveFolder = true;
        ReceiveFolder = _receiveLocation.GetDefaultAppReceiveRoot();
    }

    public async Task<SettingsSaveResult> SaveAsync(CancellationToken ct = default)
    {
        var previous = _settings.Load();
        await _identity.RenameAsync(DeviceName ?? string.Empty);
        var notificationsEnabled = NotificationsEnabled && NotificationsSupported;
        var notificationPermissionDenied = false;
        if (notificationsEnabled && !await _notifications.EnsurePermissionAsync(ct))
        {
            notificationPermissionDenied = true;
            notificationsEnabled = false;
            NotificationsEnabled = false;
        }

        var theme = ThemeIndex switch
        {
            1 => "Light",
            2 => "Dark",
            _ => "System"
        };
        var language = LanguageIndex == 1 ? "hi" : "en";
        var settings = new AppSettings(
            (int)TransferConcurrency,
            (int)HistoryRetentionDays,
            PrivacyMode,
            AutoAcceptTrustedDevices,
            theme,
            notificationsEnabled,
            ReduceMotion,
            _useDefaultReceiveFolder ? string.Empty : ReceiveFolder,
            LargerInterface,
            language,
            DeveloperOptionsEnabled);

        _settings.Save(settings);
        await _history.ApplyRetentionAsync(ct);
        _appearance.Apply(settings);
        RefreshLocalizedOptions(settings.Theme, settings.Language);
        DeviceName = _identity.DeviceName;
        IdentityFingerprint = FormatIdentityFingerprint();
#if WINDOWS
        ReceiveFolderSupport = AppText.Get("ReceiveFolderSupportWindows");
#else
        ReceiveFolderSupport = AppText.Get("ReceiveFolderSupportPrivate");
#endif
        RaiseComputedLabels();
        return new SettingsSaveResult(
            NotificationPermissionDenied: notificationPermissionDenied,
            LanguageChanged: !string.Equals(previous.Language, settings.Language, StringComparison.Ordinal));
    }

    public async Task ResetIdentityAsync(CancellationToken ct = default)
    {
        await _trustedDevices.ClearAsync(ct);
        await _identity.ResetIdentityAsync();
        DeviceName = _identity.DeviceName;
        IdentityFingerprint = FormatIdentityFingerprint();
    }

    public async Task ResetSettingsAsync(CancellationToken ct = default)
    {
        _settings.Reset();
        await _history.ApplyRetentionAsync(ct);
        _appearance.Apply(AppSettings.Default);
        await LoadAsync(ct);
    }

    public void UpdateComputedLabels()
        => RaiseComputedLabels();

    private string FormatIdentityFingerprint()
        => AppText.Format(
            "CertificateFingerprintFormat",
            Fingerprint.Pretty(Fingerprint.FromCertificate(_identity.Certificate)));

    private void RefreshLocalizedOptions(string theme, string language)
    {
        ThemeOptions = [AppText.Get("System"), AppText.Get("Light"), AppText.Get("Dark")];
        LanguageOptions = [AppText.Get("English"), AppText.Get("Hindi")];
        ThemeIndex = theme switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0
        };
        LanguageIndex = string.Equals(language, "hi", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
    }

    private void RaiseComputedLabels()
    {
        OnPropertyChanged(nameof(ConcurrencyText));
        OnPropertyChanged(nameof(RetentionText));
    }
}

public sealed record SettingsSaveResult(
    bool NotificationPermissionDenied,
    bool LanguageChanged);
