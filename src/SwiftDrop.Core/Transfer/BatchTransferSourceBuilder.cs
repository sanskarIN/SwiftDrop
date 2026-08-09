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
        foreach (var input in paths)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(input)) continue;
            var full = Path.GetFullPath(input);
            if (File.Exists(full))
            {
                await AddFileAsync(items, full, Path.GetFileName(full), ct);
            }
            else if (Directory.Exists(full))
            {
                var rootName = new DirectoryInfo(full).Name;
                foreach (var file in Directory.EnumerateFiles(full, "*", SearchOption.AllDirectories))
                {
                    ct.ThrowIfCancellationRequested();
                    var relative = Path.Combine(rootName, Path.GetRelativePath(full, file)).Replace('\\', '/');
                    await AddFileAsync(items, file, relative, ct);
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
        CancellationToken ct)
    {
        var info = new FileInfo(path);
        if (info.Length < 0 || info.Length > ProtocolConstants.MaxSingleFileBytes)
            throw new InvalidDataException("A file exceeds the SwiftDrop per-file safety limit.");

        var hash = await Hashing.Sha256FileAsync(path, ct);
        var entry = new FileManifestEntry(relativePath, info.Length, hash, info.LastWriteTimeUtc);
        items.Add(new FileTransferSource(path, entry));
    }

    private static void EnsureCount(int count)
    {
        if (count > MaxFilesPerBatch)
            throw new InvalidDataException($"A transfer can contain at most {MaxFilesPerBatch} files.");
    }
}
