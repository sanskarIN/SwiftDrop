namespace SwiftDrop.Core.Storage;

public static class StorageCapacityGuard
{
    private const long SafetyReserveBytes = 32L * 1024L * 1024L;

    public static void EnsureCapacity(string destinationPath, long incomingBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        if (incomingBytes < 0) throw new ArgumentOutOfRangeException(nameof(incomingBytes));

        var fullPath = Path.GetFullPath(destinationPath);
        var root = Path.GetPathRoot(fullPath);
        if (string.IsNullOrWhiteSpace(root)) return;

        var drive = new DriveInfo(root);
        if (!drive.IsReady) throw new IOException("Destination storage is not ready.");
        var required = checked(incomingBytes + SafetyReserveBytes);
        if (drive.AvailableFreeSpace < required)
            throw new IOException("Not enough free storage for this transfer.");
    }
}
