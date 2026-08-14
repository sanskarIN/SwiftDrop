using System.Text;

namespace SwiftDrop.Core.Protocol;

public static class TextSnippetValidator
{
    public static void Validate(string? text, DateTimeOffset expiresUtc, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidDataException("Text snippet is empty.");

        if (Encoding.UTF8.GetByteCount(text) > ProtocolConstants.MaxTextSnippetBytes)
            throw new InvalidDataException($"Text snippet exceeds {ProtocolConstants.MaxTextSnippetBytes:N0} UTF-8 bytes.");

        if (expiresUtc <= nowUtc)
            throw new InvalidDataException("Text snippet has expired.");

        var latestAllowed = nowUtc.Add(ProtocolConstants.TextSnippetLifetime).AddSeconds(30);
        if (expiresUtc > latestAllowed)
            throw new InvalidDataException("Text snippet expiry is outside the allowed window.");
    }
}
