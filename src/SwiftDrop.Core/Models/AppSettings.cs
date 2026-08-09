namespace SwiftDrop.Core.Models;

public sealed record AppSettings(
    int TransferConcurrency,
    int HistoryRetentionDays,
    bool PrivacyMode,
    bool AutoAcceptTrustedDevices,
    string Theme)
{
    public static AppSettings Default { get; } = new(
        TransferConcurrency: 2,
        HistoryRetentionDays: 30,
        PrivacyMode: false,
        AutoAcceptTrustedDevices: false,
        Theme: "System");
}
