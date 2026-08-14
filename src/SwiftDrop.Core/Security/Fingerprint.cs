using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SwiftDrop.Core.Security;

public static class Fingerprint
{
    public static string FromCertificate(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        return Convert.ToHexString(SHA256.HashData(certificate.RawData));
    }

    public static bool FixedTimeEquals(string expectedHex, string actualHex)
    {
        if (!TryNormalizeSha256(expectedHex, out var expected) ||
            !TryNormalizeSha256(actualHex, out var actual))
            return false;

        var a = Convert.FromHexString(expected);
        var b = Convert.FromHexString(actual);
        try
        {
            return CryptographicOperations.FixedTimeEquals(a, b);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(a);
            CryptographicOperations.ZeroMemory(b);
        }
    }

    public static bool TryNormalizeSha256(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var clean = value.Replace(":", string.Empty, StringComparison.Ordinal).Trim();
        if (clean.Length != 64) return false;

        byte[] bytes;
        try
        {
            bytes = Convert.FromHexString(clean);
        }
        catch (FormatException)
        {
            return false;
        }

        try
        {
            if (bytes.Length != 32) return false;
            normalized = clean.ToUpperInvariant();
            return true;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    public static string NormalizeSha256(string value)
        => TryNormalizeSha256(value, out var normalized)
            ? normalized
            : throw new FormatException("Certificate fingerprint must contain exactly 32 SHA-256 bytes.");

    public static string Pretty(string hex)
    {
        var clean = NormalizeSha256(hex);
        return string.Join(':', Enumerable.Range(0, 32).Select(i => clean.Substring(i * 2, 2)));
    }
}
