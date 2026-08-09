using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;

namespace SwiftDrop.Core.Transfer;

public static class BatchTransferSourceBuilder
{
    public const int MaxFilesPerBatch = ProtocolConstants.MaxBatchFiles;

    public static async Task<BatchTransferSource> BuildAsync(
        IEnumerable<string> paths,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var items = new List<FileTransferSource>();
        var usedRelativePaths = new HashSet<string>(StringComparer.Ordinal);
        long totalBytes = 0;

        foreach (var input in paths)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(input)) continue;
            var full = Path.GetFullPath(input);
            if (File.Exists(full))
            {
                var relative = MakeUniqueRelativePath(Path.GetFileName(full), usedRelativePaths);
                totalBytes = await AddFileAsync(items, full, relative, usedRelativePaths, totalBytes, ct);
            }
            else if (Directory.Exists(full))
            {
                var rootName = MakeUniqueRootName(new DirectoryInfo(full).Name, usedRelativePaths);
                foreach (var file in Directory.EnumerateFiles(full, "*", SearchOption.AllDirectories))
                {
                    ct.ThrowIfCancellationRequested();
                    var relative = Path.Combine(rootName, Path.GetRelativePath(full, file)).Replace('\\', '/');
                    relative = MakeUniqueRelativePath(relative, usedRelativePaths);
                    totalBytes = await AddFileAsync(items, file, relative, usedRelativePaths, totalBytes, ct);
                }
            }
            else
            {
                throw new FileNotFoundException("Transfer source does not exist.", full);
            }
        }

        if (items.Count == 0) throw new InvalidOperationException("Select at least one file to transfer.");
        return new BatchTransferSource(Guid.NewGuid().ToString("N"), items, totalBytes);
    }

    private static async Task<long> AddFileAsync(
        ICollection<FileTransferSource> items,
        string path,
        string relativePath,
        ISet<string> usedRelativePaths,
        long currentTotalBytes,
        CancellationToken ct)
    {
        EnsureCount(items.Count + 1);
        var info = new FileInfo(path);
        if (info.Length < 0 || info.Length > ProtocolConstants.MaxSingleFileBytes)
            throw new InvalidDataException("A file exceeds the SwiftDrop per-file safety limit.");

        var nextTotalBytes = checked(currentTotalBytes + info.Length);
        if (nextTotalBytes > ProtocolConstants.MaxBatchBytes)
            throw new InvalidDataException("The selected batch exceeds the SwiftDrop aggregate-size safety limit.");

        var hash = await Hashing.Sha256FileAsync(path, ct);
        var entry = ManifestValidator.ValidateEntry(new FileManifestEntry(
            relativePath,
            info.Length,
            hash,
            info.LastWriteTimeUtc));
        items.Add(new FileTransferSource(path, entry));
        usedRelativePaths.Add(relativePath);
        return nextTotalBytes;
    }

    private static string MakeUniqueRootName(string rootName, ISet<string> usedRelativePaths)
    {
        var normalized = rootName.Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(normalized)) normalized = "Folder";
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
        if (count > ProtocolConstants.MaxBatchFiles)
            throw new InvalidDataException($"A transfer can contain at most {ProtocolConstants.MaxBatchFiles} files.");
    }
}
