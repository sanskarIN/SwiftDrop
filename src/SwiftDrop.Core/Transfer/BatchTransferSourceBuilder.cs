using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Transfer;

public static class BatchTransferSourceBuilder
{
    public const int MaxFilesPerBatch = ProtocolConstants.MaxBatchFiles;
    private const int MaxDirectoriesPerBatchSource = ProtocolConstants.MaxBatchFiles * 2;
    private static readonly StringComparer PortablePathComparer = StringComparer.OrdinalIgnoreCase;

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
        var usedRelativePaths = new HashSet<string>(PortablePathComparer);
        long totalBytes = 0;

        foreach (var input in paths)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(input)) continue;
            var full = Path.GetFullPath(input);
            if (File.Exists(full))
            {
                AddPendingFile(pending, full, Path.GetFileName(full), usedRelativePaths, ref totalBytes);
            }
            else if (Directory.Exists(full))
            {
                var rootDirectory = TransferSourceSafety.GetRegularDirectory(full);
                var rootName = MakeUniqueRootName(rootDirectory.Name, usedRelativePaths);
                foreach (var file in TransferSourceEnumerator.EnumerateFiles(
                             rootDirectory.FullName,
                             ProtocolConstants.MaxBatchFiles,
                             MaxDirectoriesPerBatchSource))
                {
                    ct.ThrowIfCancellationRequested();
                    var relative = Path.Combine(rootName, Path.GetRelativePath(rootDirectory.FullName, file)).Replace('\\', '/');
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

        _ = BatchManifestValidator.Validate(items.Select(item => item.Entry).ToArray(), totalBytes);
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
        var info = TransferSourceSafety.GetRegularFile(path);
        if (info.Length < 0 || info.Length > ProtocolConstants.MaxSingleFileBytes)
            throw new InvalidDataException("A file exceeds the SwiftDrop per-file safety limit.");

        var nextTotal = checked(totalBytes + info.Length);
        if (nextTotal > ProtocolConstants.MaxBatchBytes)
            throw new InvalidDataException("The selected batch exceeds the SwiftDrop aggregate-size safety limit.");

        var safeRelativePath = FileNameSanitizer.SanitizeRelativePath(relativePath);
        safeRelativePath = MakeUniqueRelativePath(safeRelativePath, usedRelativePaths);
        if (!usedRelativePaths.Add(safeRelativePath))
            throw new InvalidDataException("Batch path deconfliction failed.");

        pending.Add(new PendingFile(info.FullName, safeRelativePath, info.Length, info.LastWriteTimeUtc));
        totalBytes = nextTotal;
    }

    private static string MakeUniqueRootName(string rootName, ISet<string> usedRelativePaths)
    {
        var normalized = FileNameSanitizer.SanitizeSegment(rootName);
        if (!RootCollides(normalized, usedRelativePaths)) return normalized;

        for (var i = 2; i <= 9999; i++)
        {
            var candidate = FileNameSanitizer.CreateCollisionSegment(normalized, i);
            if (!RootCollides(candidate, usedRelativePaths)) return candidate;
        }

        throw new IOException("Could not create a unique folder name for the batch.");
    }

    private static bool RootCollides(string rootName, IEnumerable<string> usedRelativePaths)
    {
        var platformPrefix = rootName + Path.DirectorySeparatorChar;
        var portablePrefix = rootName + '/';
        return usedRelativePaths.Any(path =>
            PortablePathComparer.Equals(path, rootName) ||
            path.StartsWith(platformPrefix, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(portablePrefix, StringComparison.OrdinalIgnoreCase));
    }

    private static string MakeUniqueRelativePath(string relativePath, ISet<string> usedRelativePaths)
    {
        var normalized = FileNameSanitizer.SanitizeRelativePath(relativePath);
        if (!usedRelativePaths.Contains(normalized)) return normalized;

        var directory = Path.GetDirectoryName(normalized);
        var fileName = Path.GetFileName(normalized);
        for (var i = 2; i <= 9999; i++)
        {
            var renamed = FileNameSanitizer.CreateCollisionSegment(fileName, i);
            var candidate = string.IsNullOrWhiteSpace(directory) ? renamed : Path.Combine(directory, renamed);
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
