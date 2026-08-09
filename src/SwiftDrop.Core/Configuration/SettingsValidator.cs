using SwiftDrop.Core.Models;

namespace SwiftDrop.Core.Configuration;

public static class SettingsValidator
{
    private static readonly HashSet<string> Themes = new(StringComparer.OrdinalIgnoreCase)
    {
        "System", "Light", "Dark"
    };

    public static AppSettings Validate(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.TransferConcurrency is < 1 or > 8)
            throw new ArgumentOutOfRangeException(nameof(settings.TransferConcurrency), "Transfer concurrency must be between 1 and 8.");
        if (settings.HistoryRetentionDays is < 0 or > 3650)
            throw new ArgumentOutOfRangeException(nameof(settings.HistoryRetentionDays), "History retention must be between 0 and 3650 days.");
        if (!Themes.Contains(settings.Theme))
            throw new ArgumentException("Theme must be System, Light, or Dark.", nameof(settings.Theme));
        return settings with { Theme = Themes.First(t => string.Equals(t, settings.Theme, StringComparison.OrdinalIgnoreCase)) };
    }
}
