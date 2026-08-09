namespace SwiftDrop.Core.Security;

public enum FileRiskLevel
{
    Normal,
    Caution,
    High
}

public static class FileRiskClassifier
{
    private static readonly HashSet<string> HighRiskExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".msi", ".msix", ".appx", ".bat", ".cmd", ".com", ".scr", ".ps1", ".psm1",
        ".vbs", ".vbe", ".js", ".jse", ".wsf", ".wsh", ".reg", ".lnk", ".jar", ".apk", ".dmg", ".pkg"
    };

    private static readonly HashSet<string> CautionExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".zip", ".7z", ".rar", ".tar", ".gz", ".iso", ".img", ".docm", ".xlsm", ".pptm"
    };

    public static FileRiskLevel Classify(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var extension = Path.GetExtension(fileName);
        if (HighRiskExtensions.Contains(extension)) return FileRiskLevel.High;
        if (CautionExtensions.Contains(extension)) return FileRiskLevel.Caution;
        return FileRiskLevel.Normal;
    }
}
