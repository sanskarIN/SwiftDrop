using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Transfer;

public static class BatchTransferSourceBuilder
{
    public const int MaxFilesPerBatch = ProtocolConstants.MaxBatchFiles;
    private const int MaxDirectoriesPerBatchSource = ProtocolConstants.MaxBatchFiles * 2;

    public static Task<BatchTransferSource> BuildAsync(
        IEnumerable<string> paths,
        CancellationToken ct = default)
        => BuildAsync(paths, Guid.NewGuid().ToString("N"), ct);

    public static async Task<BatchTransferSource> BuildAsync(
        IEnumerable<string> paths,
        string transferId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        transferId = IncomingRequestPolicy.ValidateTransferId(transferId);
        var pending = new List<PendingFile>();
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
                AddPendingFile(pending, full, relative, usedRelativePaths, ref totalBytes);
            }
            else if (Directory.Exists(full))
            {
                var rootName = MakeUniqueRootName(new DirectoryInfo(full).Name, usedRelativePaths);
                foreach (var file in TransferSourceEnumerator.EnumerateFiles(
                             full,
                             ProtocolConstants.MaxBatchFiles,
                             MaxDirectoriesPerBatchSource))
                {
                    ct.ThrowIfCancellationRequested();
                    var relative = Path.Combine(rootName, Path.GetRelativePath(full, file)).Replace('\\', '/');
                    relative = MakeUniqueRelativePath(relative, usedRelativePaths);
                    AddPendingFile(pending, file, relative, usedRelativePaths, ref totalBytes);
                }
            }
            else
            {
                throw new FileNotFoundException("Transfer source does not exist.", full);
            }
        }

        if (pending.Count == 0) throw new InvalidOperationException("Select at least one file to transfer.");

        var items = new List<FileTransferSource>(pending.Count);
        foreach (var source in pending)
        {
            ct.ThrowIfCancellationRequested();
            var hash = await Hashing.Sha256FileAsync(source.LocalPath, ct);
            var entry = ManifestValidator.ValidateEntry(new FileManifestEntry(
                source.RelativePath,
                source.Length,
                hash,
                source.LastWriteUtc));
            items.Add(new FileTransferSource(source.LocalPath, entry));
        }

        return new BatchTransferSource(transferId, items, totalBytes);
    }

    private static void AddPendingFile(
        ICollection<PendingFile> pending,
        string path,
        string relativePath,
        ISet<string> usedRelativePaths,
        ref long totalBytes)
    {
        EnsureCount(pending.Count + 1);
        var info = new FileInfo(path);
        info.Refresh();
        if (!info.Exists) throw new FileNotFoundException("Transfer source does not exist.", path);
        if ((info.Attributes & FileAttributes.ReparsePoint) != 0 || info.LinkTarget is not null)
            throw new InvalidDataException("Transfer source files cannot be symbolic links or reparse points.");
        if (info.Length < 0 || info.Length > ProtocolConstants.MaxSingleFileBytes)
            throw new InvalidDataException("A file exceeds the SwiftDrop per-file safety limit.");

        var nextTotal = checked(totalBytes + info.Length);
        if (nextTotal > ProtocolConstants.MaxBatchBytes)
            throw new InvalidDataException("The selected batch exceeds the SwiftDrop aggregate-size safety limit.");

        var safeRelativePath = FileNameSanitizer.SanitizeRelativePath(relativePath);
        safeRelativePath = MakeUniqueRelativePath(safeRelativePath, usedRelativePaths);
        if (!usedRelativePaths.Add(safeRelativePath))
            throw new InvalidDataException("Batch path deconfliction failed.");

        pending.Add(new PendingFile(path, safeRelativePath, info.Length, info.LastWriteTimeUtc));
        totalBytes = nextTotal;
    }

    private static string MakeUniqueRootName(string rootName, ISet<string> usedRelativePaths)
    {
        var normalized = FileNameSanitizer.SanitizeRelativePath(rootName.Replace('\\', '/').Trim('/'));
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

    private sealed record PendingFile(
        string LocalPath,
        string RelativePath,
        long Length,
        DateTimeOffset LastWriteUtc);
}
