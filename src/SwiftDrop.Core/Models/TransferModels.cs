namespace SwiftDrop.Core.Models;

public enum TransferDirection { Send, Receive }
public enum TransferState { Queued, Connecting, Transferring, Paused, Completed, Failed, Cancelled }
public enum CollisionPolicy { Ask, Rename, Overwrite, Skip }

public sealed record TransferItem(
    Guid Id,
    TransferDirection Direction,
    string PeerId,
    string DisplayName,
    string LocalPath,
    long TotalBytes,
    long CompletedBytes,
    TransferState State,
    string? Error = null,
    DateTimeOffset? StartedUtc = null,
    DateTimeOffset? FinishedUtc = null)
{
    public double Progress => TotalBytes <= 0 ? 0 : Math.Clamp((double)CompletedBytes / TotalBytes, 0, 1);
}

public sealed record TransferManifest(
    string ProtocolVersion,
    string TransferId,
    IReadOnlyList<FileManifestEntry> Files,
    long TotalBytes);

public sealed record FileManifestEntry(
    string RelativePath,
    long Length,
    string Sha256,
    DateTimeOffset LastWriteUtc);
