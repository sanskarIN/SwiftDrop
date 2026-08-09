using System.Globalization;
using System.Resources;

namespace SwiftDrop.App.Services;

public static class AppText
{
    private static readonly ResourceManager Resources = new(
        "SwiftDrop.App.Resources.Strings.AppStrings",
        typeof(AppText).Assembly);
    private static readonly ResourceManager MainResources = new(
        "SwiftDrop.App.Resources.Strings.MainStrings",
        typeof(AppText).Assembly);

    public static string Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var culture = CultureInfo.CurrentUICulture;
        return Resources.GetString(key, culture) ?? MainResources.GetString(key, culture) ?? key;
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
    public static string Delete => Get(nameof(Delete));
    public static string ClearFinished => Get(nameof(ClearFinished));
    public static string ClearHistory => Get(nameof(ClearHistory));
    public static string QueueDescription => Get(nameof(QueueDescription));
    public static string QueueEmpty => Get(nameof(QueueEmpty));
    public static string HistoryDescription => Get(nameof(HistoryDescription));
    public static string HistoryEmpty => Get(nameof(HistoryEmpty));
    public static string AboutSummary => Get(nameof(AboutSummary));
    public static string OpenSource => Get(nameof(OpenSource));
    public static string OpenRepository => Get(nameof(OpenRepository));
    public static string OpenCreatorProfile => Get(nameof(OpenCreatorProfile));
    public static string SupportDevelopment => Get(nameof(SupportDevelopment));
    public static string SupportDevelopmentDescription => Get(nameof(SupportDevelopmentDescription));
    public static string BuyMeACoffee => Get(nameof(BuyMeACoffee));
    public static string ContactAndSecurity => Get(nameof(ContactAndSecurity));
    public static string SecurityReportNotice => Get(nameof(SecurityReportNotice));
    public static string Privacy => Get(nameof(Privacy));
    public static string PrivacySummary => Get(nameof(PrivacySummary));
    public static string RepositoryLabel => Get(nameof(RepositoryLabel));
    public static string CreatorLabel => Get(nameof(CreatorLabel));
    public static string LicenseLabel => Get(nameof(LicenseLabel));
    public static string BusinessLabel => Get(nameof(BusinessLabel));
    public static string SupportLabel => Get(nameof(SupportLabel));
    public static string SwiftDropIconDescription => Get(nameof(SwiftDropIconDescription));
    public static string OpenRepositoryDescription => Get(nameof(OpenRepositoryDescription));
    public static string OpenCreatorProfileDescription => Get(nameof(OpenCreatorProfileDescription));
    public static string BuyMeACoffeeDescription => Get(nameof(BuyMeACoffeeDescription));
}
