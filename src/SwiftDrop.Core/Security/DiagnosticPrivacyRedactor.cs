using System.Net;

namespace SwiftDrop.Core.Security;

public static class DiagnosticPrivacyRedactor
{
    public static string Redact(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(' ', tokens.Select(RedactToken));
    }

    private static string RedactToken(string token)
    {
        var candidate = token.Trim('(', ')', '[', ']', '{', '}', ',', ';', '.', '"', '\'');
        if (candidate.Length == 0) return token;
        if (candidate.Contains('@', StringComparison.Ordinal) ||
            candidate.Contains('\\', StringComparison.Ordinal) ||
            candidate.Contains('/', StringComparison.Ordinal) ||
            candidate.StartsWith("swiftdrop:", StringComparison.OrdinalIgnoreCase) ||
            Guid.TryParse(candidate, out _) ||
            IPAddress.TryParse(TrimIpv6Brackets(candidate), out _) ||
            LooksLikeSha256Fingerprint(candidate))
            return "[redacted]";
        return token;
    }

    private static string TrimIpv6Brackets(string value)
        => value.Length >= 2 && value[0] == '[' && value[^1] == ']'
            ? value[1..^1]
            : value;

    private static bool LooksLikeSha256Fingerprint(string value)
    {
        var compact = value.Replace(":", string.Empty, StringComparison.Ordinal);
        return compact.Length == 64 && compact.All(Uri.IsHexDigit);
    }
}
