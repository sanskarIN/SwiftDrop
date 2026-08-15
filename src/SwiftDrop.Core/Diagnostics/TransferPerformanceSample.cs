namespace SwiftDrop.Core.Diagnostics;

public sealed record TransferPerformanceSample(
    DateTimeOffset TimestampUtc,
    long LogicalSizeBytes,
    long DurationMilliseconds,
    long MeasuredBytes)
{
    public bool IsValid =>
        TimestampUtc >= DateTimeOffset.UnixEpoch &&
        LogicalSizeBytes >= 0 &&
        DurationMilliseconds > 0 &&
        MeasuredBytes > 0 &&
        MeasuredBytes <= LogicalSizeBytes;
}
