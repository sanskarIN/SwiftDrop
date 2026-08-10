using System.Security.Cryptography;
using System.Text;

namespace SwiftDrop.Core.Security;

public static class ReceiveRootKey
{
    public static string Create(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        var normalized = Path.GetFullPath(root)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (PathComparisonPolicy.UsesCaseInsensitivePaths)
            normalized = normalized.ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }
}
