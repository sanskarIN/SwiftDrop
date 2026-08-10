using System.Text;

namespace SwiftDrop.Core.Security;

public static class Utf8TextLimiter
{
    public static string Truncate(string value, int maximumBytes)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (maximumBytes < 0) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        if (value.Length == 0 || maximumBytes == 0) return string.Empty;
        if (Encoding.UTF8.GetByteCount(value) <= maximumBytes) return value;

        var utf16Length = 0;
        var utf8Length = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            var runeBytes = rune.Utf8SequenceLength;
            if (utf8Length > maximumBytes - runeBytes) break;
            utf8Length += runeBytes;
            utf16Length += rune.Utf16SequenceLength;
        }
        return value[..utf16Length];
    }
}
