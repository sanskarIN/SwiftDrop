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
        if ((info.Attributes & FileAttributes.ReparsePoint) != 0 || info.LinkTarget is not null)
            throw new InvalidDataException("Transfer source files cannot be symbolic links or reparse points.");
        return info;
    }

    public static DirectoryInfo GetRegularDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var info = new DirectoryInfo(Path.GetFullPath(path));
        info.Refresh();
        if (!info.Exists)
            throw new DirectoryNotFoundException($"Transfer source directory does not exist: {info.FullName}");
        if ((info.Attributes & FileAttributes.ReparsePoint) != 0 || info.LinkTarget is not null)
            throw new InvalidDataException("Transfer source directories cannot be symbolic links or reparse points.");
        return info;
    }
}
