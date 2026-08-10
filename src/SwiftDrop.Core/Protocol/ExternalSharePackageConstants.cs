namespace SwiftDrop.Core.Protocol;

public static class ExternalSharePackageConstants
{
    public const string CurrentVersion = "1";
    public const int MaximumItems = 64;
    public const string AppleAppGroupId = "group.in.sanskar.swiftdrop";
    public const string InboxDirectoryName = "ShareInbox";
    public static readonly TimeSpan MaximumPackageAge = TimeSpan.FromHours(24);
    public static readonly TimeSpan MaximumFutureClockSkew = TimeSpan.FromMinutes(5);
}
