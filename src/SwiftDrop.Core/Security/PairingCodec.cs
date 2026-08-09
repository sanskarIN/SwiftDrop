using System.Text;
using System.Text.Json;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;

namespace SwiftDrop.Core.Security;

public static class PairingCodec
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string Encode(PairingPayload payload)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, Json);
        return $"swiftdrop://pair?p={Base64UrlEncode(bytes)}";
    }

    public static PairingPayload Decode(string text, DateTimeOffset? now = null)
    {
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || uri.Scheme != "swiftdrop" || uri.Host != "pair")
            throw new FormatException("Invalid SwiftDrop pairing link.");

        var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2)).ToDictionary(p => Uri.UnescapeDataString(p[0]), p => p.Length > 1 ? Uri.UnescapeDataString(p[1]) : string.Empty);
        if (!query.TryGetValue("p", out var encoded)) throw new FormatException("Pairing payload missing.");
        var payload = JsonSerializer.Deserialize<PairingPayload>(Base64UrlDecode(encoded), Json) ?? throw new FormatException("Pairing payload invalid.");
        if (payload.Version != ProtocolConstants.CurrentVersion) throw new NotSupportedException("Unsupported pairing protocol version.");
        if (payload.ExpiresUnixSeconds < (now ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds()) throw new InvalidOperationException("Pairing link expired.");
        if (payload.Port is <= 0 or > 65535) throw new FormatException("Invalid port.");
        if (payload.Nonce.Length < 16) throw new FormatException("Invalid nonce.");
        return payload;
    }

    public static string CreateNonce()
    {
        Span<byte> bytes = stackalloc byte[18];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static byte[] Base64UrlDecode(string value)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        s += new string('=', (4 - s.Length % 4) % 4);
        return Convert.FromBase64String(s);
    }
}
