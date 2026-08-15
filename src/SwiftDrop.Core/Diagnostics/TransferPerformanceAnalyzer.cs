using SwiftDrop.Core.Models;
using SwiftDrop.Core.Storage;

namespace SwiftDrop.Core.Diagnostics;

public static class TransferPerformanceAnalyzer
{
    public static TransferPerformanceSummary Summarize(IEnumerable<TransferHistoryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var totalRecords = 0;
        var completedRecords = 0;
        long completedBytes = 0;
        var measuredTransfers = 0;
        long measuredBytes = 0;
        long measuredDurationMilliseconds = 0;

        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            totalRecords++;
            if (!string.Equals(entry.Status, "completed", StringComparison.Ordinal))
                continue;

            completedRecords++;
            completedBytes = SaturatingAdd(completedBytes, Math.Max(0, entry.SizeBytes));

            if (!IsValidMeasurement(entry))
                continue;

            measuredTransfers++;
            measuredBytes = SaturatingAdd(measuredBytes, entry.MeasuredBytes!.Value);
            measuredDurationMilliseconds = SaturatingAdd(measuredDurationMilliseconds, entry.DurationMilliseconds!.Value);
        }

        var averageBytesPerSecond = measuredDurationMilliseconds <= 0
            ? 0d
            : measuredBytes * 1000d / measuredDurationMilliseconds;

        return new TransferPerformanceSummary(
            totalRecords,
            completedRecords,
            completedBytes,
            measuredTransfers,
            measuredBytes,
            measuredDurationMilliseconds,
            averageBytesPerSecond);
    }

    public static double BytesPerSecond(TransferHistoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (!IsValidMeasurement(entry))
            return 0d;

        return entry.MeasuredBytes!.Value * 1000d / entry.DurationMilliseconds!.Value;
    }

    public static TransferPerformanceMeasurement? NormalizeOptionalMeasurement(
        TimeSpan? duration,
        long logicalSizeBytes,
        long? measuredBytes)
    {
        if (duration is null || duration.Value <= TimeSpan.Zero)
            return null;
        if (duration.Value > TimeSpan.FromMilliseconds(TransferHistoryStore.MaxDurationMilliseconds))
            return null;
        if (logicalSizeBytes < 0 || measuredBytes is not > 0 || measuredBytes > logicalSizeBytes)
            return null;

        var durationMilliseconds = (long)Math.Ceiling(duration.Value.TotalMilliseconds);
        return durationMilliseconds is > 0 and <= TransferHistoryStore.MaxDurationMilliseconds
            ? new TransferPerformanceMeasurement(durationMilliseconds, measuredBytes.Value)
            : null;
    }

    private static bool IsValidMeasurement(TransferHistoryEntry entry)
        => string.Equals(entry.Status, "completed", StringComparison.Ordinal) &&
           entry.SizeBytes >= 0 &&
           entry.MeasuredBytes is > 0 &&
           entry.MeasuredBytes <= entry.SizeBytes &&
           entry.DurationMilliseconds is > 0 and <= TransferHistoryStore.MaxDurationMilliseconds;

    private static long SaturatingAdd(long left, long right)
        => right > 0 && left > long.MaxValue - right ? long.MaxValue : left + right;
}

public sealed record TransferPerformanceMeasurement(long DurationMilliseconds, long MeasuredBytes);

public sealed record TransferPerformanceSummary(
    int TotalRecords,
    int CompletedRecords,
    long CompletedBytes,
    int MeasuredTransfers,
    long MeasuredBytes,
    long MeasuredDurationMilliseconds,
    double AverageBytesPerSecond)
{
    public bool HasMeasurements => MeasuredTransfers > 0 && AverageBytesPerSecond > 0d;
}
