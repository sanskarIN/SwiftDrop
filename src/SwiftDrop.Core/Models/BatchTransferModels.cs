namespace SwiftDrop.Core.Models;

public sealed record FileTransferSource(
    string LocalPath,
    FileManifestEntry Entry);

public sealed record BatchTransferSource(
    string TransferId,
    IReadOnlyList<FileTransferSource> Items,
    long TotalBytes)
{
    public int FileCount => Items.Count;
}

public sealed record BatchItemPlan(
    string RelativePath,
    long ResumeOffset,
    bool Accepted,
    string? Message = null);

public sealed record BatchTransferResponse(
    bool Accepted,
    IReadOnlyList<BatchItemPlan> Items,
    string? Message = null);
