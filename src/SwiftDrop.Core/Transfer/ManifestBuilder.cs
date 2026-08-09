using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;

namespace SwiftDrop.Core.Transfer;

public static class ManifestBuilder
{
    public static async Task<TransferManifest> BuildAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        var entries = new List<FileManifestEntry>();
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(path))
            {
                var info = new FileInfo(path);
                ValidateLength(info.Length);
                entries.Add(new(Path.GetFileName(path), info.Length, await Hashing.Sha256FileAsync(path, cancellationToken), info.LastWriteTimeUtc));
            }
            else if (Directory.Exists(path))
            {
                var rootName = new DirectoryInfo(path).Name;
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var info = new FileInfo(file);
                    ValidateLength(info.Length);
                    var rel = Path.Combine(rootName, Path.GetRelativePath(path, file)).Replace('\\', '/');
                    entries.Add(new(rel, info.Length, await Hashing.Sha256FileAsync(file, cancellationToken), info.LastWriteTimeUtc));
                }
            }
            else throw new FileNotFoundException("Transfer source does not exist.", path);
        }
        return new(ProtocolConstants.CurrentVersion, Guid.NewGuid().ToString("N"), entries, entries.Sum(x => x.Length));
    }

    private static void ValidateLength(long length)
    {
        if (length < 0 || length > ProtocolConstants.MaxSingleFileBytes) throw new InvalidDataException("File exceeds SwiftDrop safety limit.");
    }
}
