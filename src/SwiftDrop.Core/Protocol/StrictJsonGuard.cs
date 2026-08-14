using System.Text;
using System.Text.Json;

namespace SwiftDrop.Core.Protocol;

public static class StrictJsonGuard
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static void Validate(ReadOnlyMemory<byte> utf8Json, int maxDepth)
    {
        if (maxDepth <= 0) throw new ArgumentOutOfRangeException(nameof(maxDepth));

        try
        {
            _ = StrictUtf8.GetCharCount(utf8Json.Span);
        }
        catch (DecoderFallbackException ex)
        {
            throw new JsonException("Protocol JSON is not valid UTF-8.", ex);
        }

        using var document = JsonDocument.Parse(utf8Json, new JsonDocumentOptions
        {
            MaxDepth = maxDepth,
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow
        });
        EnsureNoDuplicateProperties(document.RootElement);
    }

    private static void EnsureNoDuplicateProperties(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in element.EnumerateObject())
                {
                    if (!names.Add(property.Name))
                        throw new InvalidDataException("Protocol JSON contains duplicate property names.");
                    EnsureNoDuplicateProperties(property.Value);
                }
                break;
            }
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    EnsureNoDuplicateProperties(item);
                break;
        }
    }
}
