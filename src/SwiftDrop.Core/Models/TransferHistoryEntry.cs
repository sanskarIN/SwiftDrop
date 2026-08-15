namespace SwiftDrop.Core.Models;

public sealed record TransferHistoryEntry(
    string Id,
    string Direction,
    string PeerDeviceName,
    string FileName,
    long SizeBytes,
    DateTimeOffset TimestampUtc,
    string Status,
    bool IntegrityVerified,
    long? DurationMilliseconds = null,
    long? MeasuredBytes = null);
