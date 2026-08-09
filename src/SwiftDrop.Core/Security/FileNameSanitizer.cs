namespace SwiftDrop.Core.Security;

public static class FileNameSanitizer
{
    private static readonly char[] AdditionalInvalid = ['<', '>', ':', '"', '|', '?', '*'];

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
            .Trim()
            .Where(ch => !char.IsControl(ch) && !invalid.Contains(ch))
            .ToArray();
        var result = new string(chars).TrimEnd('.', ' ');
        if (string.IsNullOrWhiteSpace(result)) result = "unnamed";
        if (result.Length > 180)
        {
            var extension = Path.GetExtension(result);
            var stemLength = Math.Max(1, 180 - extension.Length);
            result = result[..Math.Min(stemLength, result.Length)] + extension;
        }
        return result;
    }
}
