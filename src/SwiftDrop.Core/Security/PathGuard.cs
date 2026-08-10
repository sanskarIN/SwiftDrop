namespace SwiftDrop.Core.Security;

public static class PathGuard
{
    public static string ResolveUnderRoot(string root, string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        if (string.IsNullOrWhiteSpace(relativePath)) throw new InvalidDataException("Empty path.");
        if (IsPortableRooted(relativePath)) throw new InvalidDataException("Rooted paths are not allowed.");
        if (relativePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0) throw new InvalidDataException("Invalid path.");

        var portableRelative = NormalizePortableSeparators(relativePath);
        var segments = portableRelative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
            throw new InvalidDataException("Path traversal attempt rejected.");

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
        if (!File.Exists(path) && !Directory.Exists(path)) return path;
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        for (var i = 1; i < 10000; i++)
        {
            var candidate = Path.Combine(directory, $"{name} ({i}){ext}");
            if (!File.Exists(candidate) && !Directory.Exists(candidate)) return candidate;
        }
        throw new IOException("Could not resolve destination filename collision.");
    }

    private static bool IsPortableRooted(string path)
    {
        if (Path.IsPathRooted(path)) return true;
        if (path[0] is '/' or '\\') return true;
        if (path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':') return true;
        return false;
    }

    private static string NormalizePortableSeparators(string path)
        => path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
}
