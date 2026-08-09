namespace SwiftDrop.Core.Security;

public static class PathComparisonPolicy
{
    public static bool UsesCaseInsensitivePaths
        => OperatingSystem.IsWindows() ||
           OperatingSystem.IsMacOS() ||
           OperatingSystem.IsMacCatalyst() ||
           OperatingSystem.IsIOS();

    public static StringComparer Comparer
        => UsesCaseInsensitivePaths ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    public static StringComparison Comparison
        => UsesCaseInsensitivePaths ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
