namespace SwiftDrop.Core.Models;

public sealed record TransferQueueMetadataEntry(
    string Id,
    string Label,
    string State,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? StartedUtc = null,
    DateTimeOffset? FinishedUtc = null,
    string? ErrorCode = null,
    string OperationKind = "Transfer",
    DateTimeOffset? UpdatedUtc = null,
    int ProgressBasisPoints = 0,
    int? ItemCount = null,
    int? CompletedItemCount = null)
{
    public double ProgressFraction => ProgressBasisPoints / 10_000d;
}
