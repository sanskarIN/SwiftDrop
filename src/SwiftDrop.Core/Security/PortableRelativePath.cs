namespace SwiftDrop.Core.Security;

public static class PortableRelativePath
{
    public const int MaximumSegments = 64;

    public static string[] GetSegments(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        if (IsRooted(relativePath))
            throw new InvalidDataException("Rooted paths are not allowed.");

        var normalized = NormalizeSeparators(relativePath);
        var segments = normalized.Split('/', StringSplitOptions.None);
        if (segments.Length == 0 || segments.Any(segment => segment.Length == 0 || segment is "." or ".."))
            throw new InvalidDataException("Relative path contains an empty or traversal segment.");
        if (segments.Length > MaximumSegments)
            throw new InvalidDataException("Relative path contains too many nested segments.");
        return segments;
    }

    public static bool IsRooted(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (Path.IsPathRooted(path)) return true;
        if (path[0] is '/' or '\\') return true;
        return path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':';
    }

    public static string NormalizeSeparators(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return path.Replace('\\', '/');
    }
}
