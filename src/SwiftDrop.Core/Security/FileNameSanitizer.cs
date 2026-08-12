using System.Text;

namespace SwiftDrop.Core.Security;

public static class FileNameSanitizer
{
    public const int MaximumSegmentLength = 180;
    private static readonly char[] AdditionalInvalid = ['/', '\\', '<', '>', ':', '"', '|', '?', '*'];
    private static readonly HashSet<string> ReservedWindowsDeviceNames = CreateReservedWindowsDeviceNames();

    public static string SanitizeRelativePath(string relativePath)
    {
        var segments = PortableRelativePath.GetSegments(relativePath);
        var safe = segments.Select(SanitizeSegment).ToArray();
        return Path.Combine(safe);
    }

    public static string GetPortableCollisionKey(string relativePath)
    {
        var sanitized = SanitizeRelativePath(relativePath)
            .Replace('\\', '/')
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/')
            .Normalize(NormalizationForm.FormC);
        return sanitized;
    }

    public static string SanitizeSegment(string segment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(segment);
        if (segment is "." or "..") throw new InvalidDataException("Traversal path segments are not allowed.");

        var invalid = Path.GetInvalidFileNameChars().Concat(AdditionalInvalid).ToHashSet();
        var chars = segment
            .Normalize(NormalizationForm.FormC)
            .Trim()
            .Where(ch => !char.IsControl(ch) && !invalid.Contains(ch))
            .ToArray();
        var result = new string(chars).TrimEnd('.', ' ');
        if (string.IsNullOrWhiteSpace(result)) result = "unnamed";

        result = AvoidReservedWindowsDeviceName(result);
        result = BoundSegment(result, MaximumSegmentLength).TrimEnd('.', ' ');
        if (string.IsNullOrWhiteSpace(result)) result = "unnamed";

        result = AvoidReservedWindowsDeviceName(result);
        if (result.Length > MaximumSegmentLength)
            result = TakeUtf16Safe(result, MaximumSegmentLength).TrimEnd('.', ' ');
        return string.IsNullOrWhiteSpace(result) ? "unnamed" : result;
    }

    private static string BoundSegment(string value, int maximumLength)
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
