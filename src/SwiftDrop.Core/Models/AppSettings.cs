namespace SwiftDrop.Core.Models;

public sealed record AppSettings(
    int TransferConcurrency,
    int HistoryRetentionDays,
    bool PrivacyMode,
    bool AutoAcceptTrustedDevices,
    string Theme,
    bool NotificationsEnabled = true,
    bool ReduceMotion = false,
    string DefaultReceiveFolder = "",
    bool LargerInterface = false,
    string Language = "en",
    bool DeveloperOptionsEnabled = false)
{
    public static AppSettings Default { get; } = new(
        TransferConcurrency: 2,
        HistoryRetentionDays: 30,
        PrivacyMode: false,
        AutoAcceptTrustedDevices: false,
        Theme: "System",
        NotificationsEnabled: true,
        ReduceMotion: false,
        DefaultReceiveFolder: "",
        LargerInterface: false,
        Language: "en",
        DeveloperOptionsEnabled: false);
}
