using System.Text.Json;
using System.Text.Json.Serialization;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Networking;
using SwiftDrop.Core.Protocol;

namespace SwiftDrop.Core.Security;

public static class PairingCodec
{
    private const int MaxLinkLength = 16_384;
    private const int MaxPayloadTextLength = 12_000;
    private const int PairingJsonMaxDepth = 16;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        MaxDepth = PairingJsonMaxDepth,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static string Encode(PairingPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, Json);
        if (bytes.Length > ProtocolConstants.HeaderLimitBytes)
            throw new InvalidDataException("Pairing payload is too large.");
        return $"swiftdrop://pair?p={Base64UrlEncode(bytes)}";
    }

    public static PairingPayload Decode(string text, DateTimeOffset? now = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (text.Length > MaxLinkLength) throw new FormatException("SwiftDrop pairing link is too large.");
        if (!Uri.TryCreate(text.Trim(), UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, "swiftdrop", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(uri.Host, "pair", StringComparison.OrdinalIgnoreCase) ||
            !uri.IsDefaultPort ||
            uri.AbsolutePath is not ("" or "/") ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            !string.IsNullOrEmpty(uri.UserInfo))
            throw new FormatException("Invalid SwiftDrop pairing link.");

        var encoded = ReadSinglePayloadParameter(uri.Query);
        if (encoded.Length is 0 or > MaxPayloadTextLength || !IsCanonicalBase64UrlText(encoded))
            throw new FormatException("Pairing payload has an invalid Base64URL representation.");

        PairingPayload payload;
        try
        {
            var decoded = Base64UrlDecode(encoded);
            if (decoded.Length > ProtocolConstants.HeaderLimitBytes)
                throw new FormatException("Pairing payload is too large.");
            if (!string.Equals(Base64UrlEncode(decoded), encoded, StringComparison.Ordinal))
                throw new FormatException("Pairing payload is not canonically encoded.");

            StrictJsonGuard.Validate(decoded, PairingJsonMaxDepth);
            payload = JsonSerializer.Deserialize<PairingPayload>(decoded, Json)
                ?? throw new FormatException("Pairing payload is invalid.");
        }
        catch (Exception ex) when (ex is FormatException or JsonException or InvalidDataException)
        {
            throw new FormatException("Pairing payload is invalid.", ex);
        }

        return Validate(payload, now ?? DateTimeOffset.UtcNow);
    }

    public static PairingPayload Validate(PairingPayload payload, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (!string.Equals(payload.Version, ProtocolConstants.CurrentVersion, StringComparison.Ordinal))
            throw new NotSupportedException("Unsupported pairing protocol version.");
        if (!IsBoundedIdentifier(payload.DeviceId, 128))
            throw new FormatException("Invalid pairing device ID.");
        if (!IsBoundedDisplayName(payload.DeviceName, 64))
            throw new FormatException("Invalid pairing device name.");

        var host = LocalAddressPolicy.ParseAndValidate(payload.Host).ToString();
        if (payload.Port is < 1 or > 65_535)
            throw new FormatException("Invalid pairing port.");
        var fingerprint = Fingerprint.NormalizeSha256(payload.CertificateFingerprint);
        if (!IsValidNonce(payload.Nonce))
            throw new FormatException("Invalid pairing nonce.");

        DateTimeOffset expiresUtc;
        try
        {
            expiresUtc = DateTimeOffset.FromUnixTimeSeconds(payload.ExpiresUnixSeconds);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new FormatException("Invalid pairing expiry.", ex);
        }
        if (expiresUtc <= nowUtc)
            throw new InvalidOperationException("Pairing link expired.");
        if (expiresUtc > nowUtc.Add(ProtocolConstants.PairingLifetime).AddMinutes(1))
            throw new InvalidOperationException("Pairing link lifetime exceeds the protocol limit.");

        return payload with
        {
            Host = host,
            CertificateFingerprint = fingerprint,
            DeviceId = payload.DeviceId.Trim(),
            DeviceName = payload.DeviceName.Trim()
        };
    }

    public static string CreateNonce()
    {
        Span<byte> bytes = stackalloc byte[18];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string ReadSinglePayloadParameter(string query)
    {
        if (string.IsNullOrEmpty(query) || query[0] != '?')
            throw new FormatException("Pairing payload missing.");

        var raw = query[1..];
        if (raw.Length == 0)
            throw new FormatException("Pairing payload missing.");

        string? payload = null;
        foreach (var pair in raw.Split('&', StringSplitOptions.None))
        {
            if (pair.Length == 0)
                throw new FormatException("Empty pairing-link query parameter.");

            var separator = pair.IndexOf('=');
            if (separator <= 0 || separator != pair.LastIndexOf('='))
                throw new FormatException("Invalid pairing-link query parameter.");

            var key = pair[..separator];
            var value = pair[(separator + 1)..];
            if (!string.Equals(key, "p", StringComparison.Ordinal))
                throw new FormatException("Unexpected pairing-link parameter.");
            if (payload is not null)
                throw new FormatException("Duplicate pairing payload parameter.");
            payload = value;
        }
        return payload ?? throw new FormatException("Pairing payload missing.");
    }

    private static bool IsBoundedIdentifier(string? value, int maxLength)
        => !string.IsNullOrWhiteSpace(value) &&
           value.Length <= maxLength &&
           !value.Any(char.IsControl);

    private static bool IsBoundedDisplayName(string? value, int maxLength)
        => IsBoundedIdentifier(value, maxLength) &&
           value!.Trim().Length > 0;

    private static bool IsValidNonce(string? nonce)
        => !string.IsNullOrWhiteSpace(nonce) &&
           nonce.Length is >= 16 and <= 128 &&
           nonce.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_');

    private static bool IsCanonicalBase64UrlText(string value)
        => value.Length % 4 != 1 &&
           value.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_');

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        if (value.Length % 4 == 1) throw new FormatException("Invalid base64url length.");
        var s = value.Replace('-', '+').Replace('_', '/');
        s += new string('=', (4 - s.Length % 4) % 4);
        return Convert.FromBase64String(s);
    }
}
