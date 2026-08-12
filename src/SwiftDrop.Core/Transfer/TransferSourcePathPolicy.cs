using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Transfer;

public static class TransferSourcePathPolicy
{
    public static bool Exists(string? path)
        => !string.IsNullOrWhiteSpace(path) && (File.Exists(path) || Directory.Exists(path));

    public static string[] ExistingDistinct(IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var result = new List<string>();
        var seen = new HashSet<string>(PathComparisonPolicy.Comparer);

        foreach (var path in paths)
        {
            if (!Exists(path)) continue;
            var full = Path.GetFullPath(path);
            if (seen.Add(full)) result.Add(full);
        }

        return result.ToArray();
    }

    public static (string Name, long Length) GetHistoryMetadata(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (File.Exists(path))
        {
            var file = new FileInfo(path);
            return (file.Name, file.Length);
        }
        if (Directory.Exists(path))
        {
            var directory = new DirectoryInfo(path);
            return (directory.Name, 0);
        }
        throw new FileNotFoundException("Transfer source does not exist.", path);
    }
}
