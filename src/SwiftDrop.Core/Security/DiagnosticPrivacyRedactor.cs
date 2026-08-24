using System.Net;

namespace SwiftDrop.Core.Security;

public static class DiagnosticPrivacyRedactor
{
    public static string Redact(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var tokens = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(' ', tokens.Select(RedactToken));
    }

    private static string RedactToken(string token)
    {
        var candidate = token.Trim('(', ')', '{', '}', ',', ';', '.', '"', '\'');
        if (candidate.Length == 0) return token;
        if (candidate.Contains('@') ||
            candidate.Contains('\\') ||
            candidate.Contains('/') ||
            candidate.StartsWith("swiftdrop:", StringComparison.OrdinalIgnoreCase) ||
            Guid.TryParse(candidate.Trim('[', ']'), out _) ||
            LooksLikeIpOrEndpoint(candidate) ||
            LooksLikeSha256Fingerprint(candidate))
            return "[redacted]";
        return token;
    }

    private static bool LooksLikeIpOrEndpoint(string value)
    {
        var plain = value.Trim('[', ']');
        if (IPAddress.TryParse(plain, out _)) return true;

        if (value.Length > 0 && value[0] == '[')
        {
            var close = value.IndexOf(']');
            if (close > 1 && IPAddress.TryParse(value[1..close], out _))
            {
                if (close == value.Length - 1) return true;
                if (close + 2 < value.Length && value[close + 1] == ':' &&
                    ushort.TryParse(value[(close + 2)..], out var port) && port > 0)
                    return true;
            }
        }

        var separator = value.LastIndexOf(':');
        if (separator > 0 && separator < value.Length - 1 &&
            ushort.TryParse(value[(separator + 1)..], out var endpointPort) && endpointPort > 0 &&
            IPAddress.TryParse(value[..separator], out _))
            return true;

        return false;
    }

    private static bool LooksLikeSha256Fingerprint(string value)
    {
        var compact = value.Replace(":", string.Empty, StringComparison.Ordinal);
        return compact.Length == 64 && compact.All(Uri.IsHexDigit);
    }
}
