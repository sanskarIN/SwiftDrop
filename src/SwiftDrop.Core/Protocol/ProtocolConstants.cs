namespace SwiftDrop.Core.Protocol;

public static class ProtocolConstants
{
    public const string CurrentVersion = "1";
    public const int DefaultPort = 47821;
    public const int ChunkSize = 256 * 1024;
    public const long MaxSingleFileBytes = 100L * 1024 * 1024 * 1024;
    public const int HeaderLimitBytes = 64 * 1024;
    public static readonly TimeSpan PairingLifetime = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(45);
}
