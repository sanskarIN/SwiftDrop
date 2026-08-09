using System.Globalization;
using System.Resources;

namespace SwiftDrop.App.Services;

public static class AppText
{
    private static readonly ResourceManager Resources = new(
        "SwiftDrop.App.Resources.Strings.AppStrings",
        typeof(AppText).Assembly);

    public static string Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    }

    public static string AppName => Get(nameof(AppName));
    public static string Tagline => Get(nameof(Tagline));
    public static string NearbyDevices => Get(nameof(NearbyDevices));
    public static string TransferQueue => Get(nameof(TransferQueue));
    public static string TransferHistory => Get(nameof(TransferHistory));
    public static string Settings => Get(nameof(Settings));
    public static string Diagnostics => Get(nameof(Diagnostics));
    public static string About => Get(nameof(About));
    public static string MadeBy => Get(nameof(MadeBy));
    public static string Accept => Get(nameof(Accept));
    public static string Reject => Get(nameof(Reject));
    public static string Cancel => Get(nameof(Cancel));
    public static string Pause => Get(nameof(Pause));
    public static string Resume => Get(nameof(Resume));
    public static string Refresh => Get(nameof(Refresh));
}
