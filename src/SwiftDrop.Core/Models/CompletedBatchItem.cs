namespace SwiftDrop.Core.Models;

public sealed record CompletedBatchItem(
    string TransferId,
    string SourceRelativePath,
    string ReceiveRootKey,
    string DestinationRelativePath,
    long Length,
    string Sha256,
    DateTimeOffset CompletedUtc);
