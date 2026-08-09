using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SwiftDrop.Core.Security;

public static class Fingerprint
{
    public static string FromCertificate(X509Certificate2 certificate) =>
        Convert.ToHexString(SHA256.HashData(certificate.RawData));

    public static bool FixedTimeEquals(string expectedHex, string actualHex)
    {
        try
        {
            var a = Convert.FromHexString(expectedHex.Replace(":", string.Empty));
            var b = Convert.FromHexString(actualHex.Replace(":", string.Empty));
            return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
        }
        catch (FormatException) { return false; }
    }

    public static string Pretty(string hex)
    {
        var clean = hex.Replace(":", string.Empty).ToUpperInvariant();
        return string.Join(':', Enumerable.Range(0, clean.Length / 2).Select(i => clean.Substring(i * 2, 2)));
    }
}
