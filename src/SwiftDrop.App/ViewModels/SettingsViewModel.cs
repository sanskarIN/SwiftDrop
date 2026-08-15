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
    private string _theme = "System";
    private string _language = "English";
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
    public string Theme { get => _theme; set => SetProperty(ref _theme, value); }
    public string Language { get => _language; set => SetProperty(ref _language, value); }
    public string ConcurrencyText => $"{(int)TransferConcurrency}";
    public string RetentionText => HistoryRetentionDays == 0 ? "Do not retain history" : $"{(int)HistoryRetentionDays} days";

    public async Task LoadAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await _identity.InitializeAsync();
        var settings = _settings.Load();
        DeviceName = _identity.DeviceName;
        IdentityFingerprint = $"Certificate fingerprint: {Fingerprint.Pretty(Fingerprint.FromCertificate(_identity.Certificate))}";
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
        Theme = settings.Theme;
        Language = string.Equals(settings.Language, "hi", StringComparison.OrdinalIgnoreCase) ? "Hindi" : "English";
        _useDefaultReceiveFolder = string.IsNullOrWhiteSpace(settings.DefaultReceiveFolder);
        ReceiveFolder = _useDefaultReceiveFolder
            ? _receiveLocation.GetDefaultAppReceiveRoot()
            : settings.DefaultReceiveFolder;
#if WINDOWS
        ReceiveFolderSupport = "Windows uses the system folder picker. SwiftDrop requests access only to the folder you choose.";
#else
        ReceiveFolderSupport = "This platform currently uses SwiftDrop's app-private Received folder. Custom external-folder selection is disabled rather than requesting broad filesystem access.";
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
        var settings = new AppSettings(
            (int)TransferConcurrency,
            (int)HistoryRetentionDays,
            PrivacyMode,
            AutoAcceptTrustedDevices,
            Theme,
            notificationsEnabled,
            ReduceMotion,
            _useDefaultReceiveFolder ? string.Empty : ReceiveFolder,
            LargerInterface,
            string.Equals(Language, "Hindi", StringComparison.Ordinal) ? "hi" : "en",
            DeveloperOptionsEnabled);

        _settings.Save(settings);
        await _history.ApplyRetentionAsync(ct);
        _appearance.Apply(settings);
        DeviceName = _identity.DeviceName;
        IdentityFingerprint = $"Certificate fingerprint: {Fingerprint.Pretty(Fingerprint.FromCertificate(_identity.Certificate))}";
        return new SettingsSaveResult(
            NotificationPermissionDenied: notificationPermissionDenied,
            LanguageChanged: !string.Equals(previous.Language, settings.Language, StringComparison.Ordinal));
    }

    public async Task ResetIdentityAsync(CancellationToken ct = default)
    {
        await _trustedDevices.ClearAsync(ct);
        await _identity.ResetIdentityAsync();
        DeviceName = _identity.DeviceName;
        IdentityFingerprint = $"Certificate fingerprint: {Fingerprint.Pretty(Fingerprint.FromCertificate(_identity.Certificate))}";
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

    private void RaiseComputedLabels()
    {
        OnPropertyChanged(nameof(ConcurrencyText));
        OnPropertyChanged(nameof(RetentionText));
    }
}

public sealed record SettingsSaveResult(
    bool NotificationPermissionDenied,
    bool LanguageChanged);
