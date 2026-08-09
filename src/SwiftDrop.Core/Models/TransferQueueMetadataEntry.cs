namespace SwiftDrop.Core.Models;

public sealed record TransferQueueMetadataEntry(
    string Id,
    string Label,
    string State,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? StartedUtc = null,
    DateTimeOffset? FinishedUtc = null,
    string? ErrorCode = null);
