using System.Text;

namespace SwiftDrop.Core.Security;

public static class FileNameSanitizer
{
    private static readonly char[] AdditionalInvalid = ['<', '>', ':', '"', '|', '?', '*'];
    private static readonly HashSet<string> ReservedWindowsDeviceNames = CreateReservedWindowsDeviceNames();

    public static string SanitizeRelativePath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        if (Path.IsPathRooted(relativePath)) throw new InvalidDataException("Rooted paths are not allowed.");

        var normalized = relativePath.Replace('\\', '/');
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) throw new InvalidDataException("Path has no usable filename.");

        var safe = segments.Select(SanitizeSegment).ToArray();
        return Path.Combine(safe);
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
        if (result.Length > 180)
        {
            var extension = Path.GetExtension(result);
            var stemLength = Math.Max(1, 180 - extension.Length);
            result = result[..Math.Min(stemLength, result.Length)] + extension;
            result = result.TrimEnd('.', ' ');
            result = AvoidReservedWindowsDeviceName(result);
        }
        return result;
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
