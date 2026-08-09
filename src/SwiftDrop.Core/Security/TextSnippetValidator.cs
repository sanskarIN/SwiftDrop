using System.Text;
using SwiftDrop.Core.Protocol;

namespace SwiftDrop.Core.Security;

public static class TextSnippetValidator
{
    public static void Validate(string? text, DateTimeOffset expiresUtc, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new InvalidDataException("Text snippet is empty.");
        var byteCount = Encoding.UTF8.GetByteCount(text);
        if (byteCount > ProtocolConstants.MaxTextSnippetBytes)
            throw new InvalidDataException($"Text snippet exceeds {ProtocolConstants.MaxTextSnippetBytes:N0} UTF-8 bytes.");
        if (expiresUtc < nowUtc) throw new InvalidDataException("Text snippet expired.");
        if (expiresUtc - nowUtc > ProtocolConstants.TextSnippetLifetime.Add(TimeSpan.FromSeconds(10)))
            throw new InvalidDataException("Text snippet expiry is too far in the future.");
    }
}
