using System.Text;

namespace SwiftDrop.Core.Security;

public static class FileNameSanitizer
{
    public const int MaximumSegmentLength = 180;
    public const int MaximumSegmentUtf8Bytes = 180;
    private static readonly char[] AdditionalInvalid = ['/', '\\', '<', '>', ':', '"', '|', '?', '*'];
    private static readonly HashSet<string> ReservedWindowsDeviceNames = CreateReservedWindowsDeviceNames();

    public static string SanitizeRelativePath(string relativePath)
    {
        var segments = PortableRelativePath.GetSegments(relativePath);
        var safe = segments.Select(SanitizeSegment).ToArray();
        return string.Join('/', safe);
    }

    public static string GetPortableCollisionKey(string relativePath)
        => SanitizeRelativePath(relativePath).Normalize(NormalizationForm.FormC);

    public static string SanitizeSegment(string segment)
    {
        ArgumentNullException.ThrowIfNull(segment);
        if (segment is "." or "..") throw new InvalidDataException("Traversal path segments are not allowed.");

        var invalid = Path.GetInvalidFileNameChars().Concat(AdditionalInvalid).ToHashSet();
        var chars = segment
            .Normalize(NormalizationForm.FormC)
            .Trim()
            .Where(ch => !char.IsControl(ch) && !invalid.Contains(ch))
            .ToArray();
        var result = new string(chars).Normalize(NormalizationForm.FormC).Trim().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(result)) result = "unnamed";

        result = AvoidReservedWindowsDeviceName(result);
        result = BoundSegmentUtf16(result, MaximumSegmentLength);
        result = BoundSegmentUtf8(result, MaximumSegmentUtf8Bytes).Trim().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(result)) result = "unnamed";

        result = AvoidReservedWindowsDeviceName(result);
        result = BoundSegmentUtf16(result, MaximumSegmentLength);
        result = BoundSegmentUtf8(result, MaximumSegmentUtf8Bytes).Trim().TrimEnd('.');
        return string.IsNullOrWhiteSpace(result) ? "unnamed" : result;
    }

    public static string CreateCollisionSegment(string segment, int index)
    {
        if (index is < 1 or > 9999) throw new ArgumentOutOfRangeException(nameof(index));
        var safe = SanitizeSegment(segment);
        var extension = Path.GetExtension(safe);
        var stem = Path.GetFileNameWithoutExtension(safe);
        var suffix = $" ({index})";
        var conventional = $"{stem}{suffix}{extension}";

        if (conventional.Length <= MaximumSegmentLength &&
            Encoding.UTF8.GetByteCount(conventional) <= MaximumSegmentUtf8Bytes)
        {
            return SanitizeSegment(conventional);
        }

        // Prefix fallback keeps the uniqueness token at the beginning, where bounded truncation cannot discard it.
        return SanitizeSegment($"({index}) {safe}");
    }

    private static string BoundSegmentUtf16(string value, int maximumLength)
    {
        if (value.Length <= maximumLength) return value;

        var extension = Path.GetExtension(value);
        if (extension.Length >= maximumLength)
            return TakeUtf16Safe(value, maximumLength);

        var stem = Path.GetFileNameWithoutExtension(value);
        var maximumStemLength = maximumLength - extension.Length;
        var boundedStem = TakeUtf16Safe(stem, maximumStemLength);
        if (boundedStem.Length == 0) boundedStem = "_";
        var bounded = boundedStem + extension;
        return bounded.Length <= maximumLength
            ? bounded
            : TakeUtf16Safe(bounded, maximumLength);
    }

    private static string BoundSegmentUtf8(string value, int maximumBytes)
    {
        if (Encoding.UTF8.GetByteCount(value) <= maximumBytes) return value;

        var extension = Path.GetExtension(value);
        var extensionBytes = Encoding.UTF8.GetByteCount(extension);
        if (extensionBytes >= maximumBytes)
            return TakeUtf8Safe(value, maximumBytes);

        var stem = Path.GetFileNameWithoutExtension(value);
        var boundedStem = TakeUtf8Safe(stem, maximumBytes - extensionBytes);
        if (boundedStem.Length == 0) boundedStem = "_";
        var bounded = boundedStem + extension;
        return Encoding.UTF8.GetByteCount(bounded) <= maximumBytes
            ? bounded
            : TakeUtf8Safe(bounded, maximumBytes);
    }

    private static string TakeUtf16Safe(string value, int maximumLength)
    {
        if (maximumLength <= 0 || value.Length == 0) return string.Empty;
        var length = Math.Min(maximumLength, value.Length);
        if (length < value.Length &&
            length > 0 &&
            char.IsHighSurrogate(value[length - 1]) &&
            char.IsLowSurrogate(value[length]))
        {
            length--;
        }
        return value[..length];
    }

    private static string TakeUtf8Safe(string value, int maximumBytes)
    {
        if (maximumBytes <= 0 || value.Length == 0) return string.Empty;
        var builder = new StringBuilder(Math.Min(value.Length, maximumBytes));
        var usedBytes = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            var runeBytes = rune.Utf8SequenceLength;
            if (usedBytes + runeBytes > maximumBytes) break;
            builder.Append(rune.ToString());
            usedBytes += runeBytes;
        }
        return builder.ToString();
    }

    private static string AvoidReservedWindowsDeviceName(string value)
    {
        var stem = Path.GetFileNameWithoutExtension(value).TrimEnd('.', ' ');
        return ReservedWindowsDeviceNames.Contains(stem) ? "_" + value : value;
    }

    private static HashSet<string> CreateReservedWindowsDeviceNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL", "CLOCK$"
        };
        for (var i = 1; i <= 9; i++)
        {
            names.Add($"COM{i}");
            names.Add($"LPT{i}");
        }
        return names;
    }
}
