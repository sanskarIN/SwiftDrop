namespace SwiftDrop.Core.Security;

public static class PathGuard
{
    public static string ResolveUnderRoot(string root, string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        var segments = PortableRelativePath.GetSegments(relativePath);
        if (relativePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            throw new InvalidDataException("Invalid path.");

        var portableRelative = Path.Combine(segments);
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(Path.Combine(fullRoot, portableRelative));
        if (!full.StartsWith(fullRoot, PathComparisonPolicy.Comparison))
            throw new InvalidDataException("Path traversal attempt rejected.");
        return full;
    }

    public static string EnsureNoReparsePointsUnderRoot(string root, string relativePath)
    {
        var full = ResolveUnderRoot(root, relativePath);
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var relative = Path.GetRelativePath(normalizedRoot, full);
        var current = normalizedRoot;

        foreach (var segment in relative.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            try
            {
                var attributes = File.GetAttributes(current);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Receive paths cannot traverse symbolic links or reparse points.");
            }
            catch (FileNotFoundException)
            {
                // A not-yet-created child is safe at this instant; callers recheck after creating parents.
            }
            catch (DirectoryNotFoundException)
            {
                // Remaining descendants cannot exist before the missing parent is created.
            }
        }

        return full;
    }

    public static string GetCollisionFreePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var full = Path.GetFullPath(path);
        if (!File.Exists(full) && !Directory.Exists(full)) return full;

        var directory = Path.GetDirectoryName(full) ?? string.Empty;
        var requestedName = Path.GetFileName(full);
        for (var i = 1; i < 10_000; i++)
        {
            var collisionName = FileNameSanitizer.CreateCollisionSegment(requestedName, i);
            var candidate = Path.Combine(directory, collisionName);
            if (!File.Exists(candidate) && !Directory.Exists(candidate)) return candidate;
        }
        throw new IOException("Could not resolve destination filename collision.");
    }
}
