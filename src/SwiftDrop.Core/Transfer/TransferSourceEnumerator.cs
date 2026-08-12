using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Transfer;

public static class TransferSourceEnumerator
{
    public static IReadOnlyList<string> EnumerateFiles(
        string rootPath,
        int maximumFiles,
        int maximumDirectories)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        if (maximumFiles <= 0) throw new ArgumentOutOfRangeException(nameof(maximumFiles));
        if (maximumDirectories <= 0) throw new ArgumentOutOfRangeException(nameof(maximumDirectories));

        var root = TransferSourceSafety.GetRegularDirectory(rootPath);
        var files = new List<string>();
        var stack = new Stack<DirectoryInfo>();
        stack.Push(root);
        var directoryCount = 0;

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            TransferSourceSafety.EnsureNotLink(current);
            if (++directoryCount > maximumDirectories)
                throw new InvalidDataException("Transfer source contains too many directories.");

            foreach (var entry in current.EnumerateFileSystemInfos())
            {
                TransferSourceSafety.EnsureNotLink(entry);
                if (entry is DirectoryInfo directory)
                {
                    stack.Push(directory);
                    continue;
                }

                if (entry is not FileInfo file) continue;
                files.Add(file.FullName);
                if (files.Count > maximumFiles)
                    throw new InvalidDataException("Transfer source contains too many files.");
            }
        }

        return files
            .OrderBy(
                path => PortableRelativePath.NormalizeSeparators(Path.GetRelativePath(root.FullName, path)),
                StringComparer.Ordinal)
            .ToArray();
    }
}
