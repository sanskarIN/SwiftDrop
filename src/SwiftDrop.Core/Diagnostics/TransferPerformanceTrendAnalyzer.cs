using SwiftDrop.Core.Models;

namespace SwiftDrop.Core.Diagnostics;

public static class TransferPerformanceTrendAnalyzer
{
    public const int DefaultWindowDays = 30;
    public const int MaxWindowDays = 3650;

    public static IReadOnlyList<TransferPerformanceTrendPoint> BuildDaily(
        IEnumerable<TransferHistoryEntry> entries,
        DateTimeOffset windowEndUtc,
        int windowDays = DefaultWindowDays)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (windowDays is < 1 or > MaxWindowDays)
            throw new ArgumentOutOfRangeException(nameof(windowDays));
        if (windowEndUtc < DateTimeOffset.UnixEpoch)
            throw new ArgumentOutOfRangeException(nameof(windowEndUtc));

        var endDate = DateOnly.FromDateTime(windowEndUtc.UtcDateTime);
        var startDate = endDate.AddDays(-(windowDays - 1));
        var buckets = new Dictionary<DateOnly, Bucket>();

        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            if (!TransferPerformanceAnalyzer.IsValidMeasurement(entry))
                continue;

            var date = DateOnly.FromDateTime(entry.TimestampUtc.UtcDateTime);
            if (date < startDate || date > endDate)
                continue;

            if (!buckets.TryGetValue(date, out var bucket))
            {
                bucket = new Bucket();
                buckets.Add(date, bucket);
            }

            bucket.Add(entry.MeasuredBytes!.Value, entry.DurationMilliseconds!.Value);
        }

        return buckets
            .OrderBy(pair => pair.Key)
            .Select(pair => pair.Value.ToPoint(pair.Key))
            .ToArray();
    }

    private sealed class Bucket
    {
        private int _measuredTransfers;
        private long _measuredBytes;
        private long _measuredDurationMilliseconds;

        public void Add(long measuredBytes, long durationMilliseconds)
        {
            if (_measuredTransfers < int.MaxValue)
                _measuredTransfers++;
            _measuredBytes = SaturatingAdd(_measuredBytes, measuredBytes);
            _measuredDurationMilliseconds = SaturatingAdd(_measuredDurationMilliseconds, durationMilliseconds);
        }

        public TransferPerformanceTrendPoint ToPoint(DateOnly dateUtc)
        {
            var averageBytesPerSecond = _measuredDurationMilliseconds <= 0
                ? 0d
                : _measuredBytes * 1000d / _measuredDurationMilliseconds;
            return new TransferPerformanceTrendPoint(
                dateUtc,
                _measuredTransfers,
                _measuredBytes,
                _measuredDurationMilliseconds,
                averageBytesPerSecond);
        }

        private static long SaturatingAdd(long left, long right)
            => right > 0 && left > long.MaxValue - right ? long.MaxValue : left + right;
    }
}

public sealed record TransferPerformanceTrendPoint(
    DateOnly DateUtc,
    int MeasuredTransfers,
    long MeasuredBytes,
    long MeasuredDurationMilliseconds,
    double AverageBytesPerSecond)
{
    public bool HasMeasurements =>
        MeasuredTransfers > 0 &&
        MeasuredBytes > 0 &&
        MeasuredDurationMilliseconds > 0 &&
        double.IsFinite(AverageBytesPerSecond) &&
        AverageBytesPerSecond > 0d;
}
