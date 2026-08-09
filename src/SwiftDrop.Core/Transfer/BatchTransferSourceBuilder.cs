using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;

namespace SwiftDrop.Core.Transfer;

public static class BatchTransferSourceBuilder
{
    public const int MaxFilesPerBatch = 2048;

    public static async Task<BatchTransferSource> BuildAsync(
        IEnumerable<string> paths,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var items = new List<FileTransferSource>();
        var usedRelativePaths = new HashSet<string>(StringComparer.Ordinal);

        foreach (var input in paths)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(input)) continue;
            var full = Path.GetFullPath(input);
            if (File.Exists(full))
            {
                var relative = MakeUniqueRelativePath(Path.GetFileName(full), usedRelativePaths);
                await AddFileAsync(items, full, relative, usedRelativePaths, ct);
            }
            else if (Directory.Exists(full))
            {
                var rootName = MakeUniqueRootName(new DirectoryInfo(full).Name, usedRelativePaths);
                foreach (var file in Directory.EnumerateFiles(full, "*", SearchOption.AllDirectories))
                {
                    ct.ThrowIfCancellationRequested();
                    var relative = Path.Combine(rootName, Path.GetRelativePath(full, file)).Replace('\\', '/');
                    relative = MakeUniqueRelativePath(relative, usedRelativePaths);
                    await AddFileAsync(items, file, relative, usedRelativePaths, ct);
                    EnsureCount(items.Count);
                }
            }
            else
            {
                throw new FileNotFoundException("Transfer source does not exist.", full);
            }

            EnsureCount(items.Count);
        }

        if (items.Count == 0) throw new InvalidOperationException("Select at least one file to transfer.");
        var total = checked(items.Sum(x => x.Entry.Length));
        return new BatchTransferSource(Guid.NewGuid().ToString("N"), items, total);
    }

    private static async Task AddFileAsync(
        ICollection<FileTransferSource> items,
        string path,
        string relativePath,
        ISet<string> usedRelativePaths,
        CancellationToken ct)
    {
        var info = new FileInfo(path);
        if (info.Length < 0 || info.Length > ProtocolConstants.MaxSingleFileBytes)
            throw new InvalidDataException("A file exceeds the SwiftDrop per-file safety limit.");

        var hash = await Hashing.Sha256FileAsync(path, ct);
        var entry = new FileManifestEntry(relativePath, info.Length, hash, info.LastWriteTimeUtc);
        items.Add(new FileTransferSource(path, entry));
        usedRelativePaths.Add(relativePath);
    }

    private static string MakeUniqueRootName(string rootName, ISet<string> usedRelativePaths)
    {
        var normalized = rootName.Replace('\\', '/').Trim('/');
        if (!usedRelativePaths.Any(path => string.Equals(path, normalized, StringComparison.Ordinal) || path.StartsWith(normalized + "/", StringComparison.Ordinal)))
            return normalized;

        for (var i = 2; i <= 9999; i++)
        {
            var candidate = $"{normalized} ({i})";
            if (!usedRelativePaths.Any(path => string.Equals(path, candidate, StringComparison.Ordinal) || path.StartsWith(candidate + "/", StringComparison.Ordinal)))
                return candidate;
        }

        throw new IOException("Could not create a unique folder name for the batch.");
    }

    private static string MakeUniqueRelativePath(string relativePath, ISet<string> usedRelativePaths)
    {
        var normalized = relativePath.Replace('\\', '/');
        if (!usedRelativePaths.Contains(normalized)) return normalized;

        var directory = Path.GetDirectoryName(normalized)?.Replace('\\', '/');
        var fileName = Path.GetFileNameWithoutExtension(normalized);
        var extension = Path.GetExtension(normalized);
        for (var i = 2; i <= 9999; i++)
        {
            var renamed = $"{fileName} ({i}){extension}";
            var candidate = string.IsNullOrWhiteSpace(directory) ? renamed : $"{directory}/{renamed}";
            if (!usedRelativePaths.Contains(candidate)) return candidate;
        }

        throw new IOException("Could not create a unique filename for the batch.");
    }

    private static void EnsureCount(int count)
    {
        if (count > MaxFilesPerBatch)
            throw new InvalidDataException($"A transfer can contain at most {MaxFilesPerBatch} files.");
    }
}
