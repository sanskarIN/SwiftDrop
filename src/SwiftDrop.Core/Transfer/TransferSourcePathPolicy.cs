using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Transfer;

public static class TransferSourcePathPolicy
{
    public static bool Exists(string? path)
        => !string.IsNullOrWhiteSpace(path) && (File.Exists(path) || Directory.Exists(path));

    public static string[] ExistingDistinct(IEnumerable<string?> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var result = new List<string>();
        var seen = new HashSet<string>(PathComparisonPolicy.Comparer);

        foreach (var path in paths)
        {
            var full = TryGetRegularSourcePath(path);
            if (full is null || !seen.Add(full)) continue;
            result.Add(full);
        }

        return result.ToArray();
    }

    public static (string Name, long Length) GetHistoryMetadata(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (File.Exists(path))
        {
            var file = TransferSourceSafety.GetRegularFile(path);
            return (file.Name, file.Length);
        }
        if (Directory.Exists(path))
        {
            var directory = TransferSourceSafety.GetRegularDirectory(path);
            return (directory.Name, 0);
        }
        throw new FileNotFoundException("Transfer source does not exist.", path);
    }

    private static string? TryGetRegularSourcePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        try
        {
            if (File.Exists(path)) return TransferSourceSafety.GetRegularFile(path).FullName;
            if (Directory.Exists(path)) return TransferSourceSafety.GetRegularDirectory(path).FullName;
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
        return null;
    }
}
