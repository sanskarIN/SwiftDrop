namespace SwiftDrop.Core.Transfer;

public static class TransferSourceSafety
{
    public static FileInfo GetRegularFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var info = new FileInfo(Path.GetFullPath(path));
        info.Refresh();
        if (!info.Exists)
            throw new FileNotFoundException("Transfer source does not exist.", info.FullName);
        EnsureNotLink(info);
        return info;
    }

    public static DirectoryInfo GetRegularDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var info = new DirectoryInfo(Path.GetFullPath(path));
        info.Refresh();
        if (!info.Exists)
            throw new DirectoryNotFoundException($"Transfer source directory does not exist: {info.FullName}");
        EnsureNotLink(info);
        return info;
    }

    public static void EnsureNotLink(FileSystemInfo entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        entry.Refresh();
        if ((entry.Attributes & FileAttributes.ReparsePoint) != 0 || entry.LinkTarget is not null)
            throw new InvalidDataException("Transfer sources cannot be symbolic links or reparse points.");
    }
}
