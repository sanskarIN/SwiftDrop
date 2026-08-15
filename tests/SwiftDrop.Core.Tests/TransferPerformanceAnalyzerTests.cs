using SwiftDrop.Core.Diagnostics;
using SwiftDrop.Core.Models;

namespace SwiftDrop.Core.Tests;

public sealed class TransferPerformanceAnalyzerTests
{
    [Fact]
    public void Summarize_UsesOnlyCompletedMeasuredTransfersForThroughput()
    {
        var now = DateTimeOffset.UtcNow;
        TransferHistoryEntry[] rows =
        [
            Entry("one", 2_000, "completed", 1_000, 2_000, now),
            Entry("two", 4_000, "completed", 2_000, 4_000, now),
            Entry("legacy", 8_000, "completed", null, null, now),
            Entry("duration-only", 1_000, "completed", 500, null, now),
            Entry("failed", 16_000, "failed", 1_000, 16_000, now),
            Entry("zero", 0, "completed", 100, 0, now)
        ];

        var summary = TransferPerformanceAnalyzer.Summarize(rows);

        Assert.Equal(6, summary.TotalRecords);
        Assert.Equal(5, summary.CompletedRecords);
        Assert.Equal(15_000, summary.CompletedBytes);
        Assert.Equal(2, summary.MeasuredTransfers);
        Assert.Equal(6_000, summary.MeasuredBytes);
        Assert.Equal(3_000, summary.MeasuredDurationMilliseconds);
        Assert.Equal(2_000d, summary.AverageBytesPerSecond, 6);
        Assert.True(summary.HasMeasurements);
    }

    [Fact]
    public void Summarize_UsesActualMeasuredBytesForResumedTransfer()
    {
        var row = Entry(
            "resumed",
            size: 10_000,
            status: "completed",
            durationMilliseconds: 1_000,
            measuredBytes: 2_500,
            DateTimeOffset.UtcNow);

        var summary = TransferPerformanceAnalyzer.Summarize([row]);

        Assert.Equal(10_000, summary.CompletedBytes);
        Assert.Equal(2_500, summary.MeasuredBytes);
        Assert.Equal(2_500d, summary.AverageBytesPerSecond, 6);
        Assert.Equal(2_500d, TransferPerformanceAnalyzer.BytesPerSecond(row), 6);
    }

    [Fact]
    public void Summarize_WithoutMeasuredRowsReturnsZeroThroughput()
    {
        var summary = TransferPerformanceAnalyzer.Summarize(
        [
            Entry("legacy", 100, "completed", null, null, DateTimeOffset.UtcNow),
            Entry("duration-only", 100, "completed", 100, null, DateTimeOffset.UtcNow),
            Entry("failed", 100, "failed", 100, 100, DateTimeOffset.UtcNow)
        ]);

        Assert.Equal(0, summary.MeasuredTransfers);
        Assert.Equal(0d, summary.AverageBytesPerSecond);
        Assert.False(summary.HasMeasurements);
    }

    [Fact]
    public void BytesPerSecond_RequiresCompletedPositiveMeasurement()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Equal(4_000d, TransferPerformanceAnalyzer.BytesPerSecond(Entry("ok", 2_000, "completed", 500, 2_000, now)), 6);
        Assert.Equal(0d, TransferPerformanceAnalyzer.BytesPerSecond(Entry("failed", 2_000, "failed", 500, 2_000, now)));
        Assert.Equal(0d, TransferPerformanceAnalyzer.BytesPerSecond(Entry("legacy", 2_000, "completed", null, null, now)));
        Assert.Equal(0d, TransferPerformanceAnalyzer.BytesPerSecond(Entry("duration-only", 2_000, "completed", 500, null, now)));
        Assert.Equal(0d, TransferPerformanceAnalyzer.BytesPerSecond(Entry("instant", 2_000, "completed", 0, 2_000, now)));
    }

    [Fact]
    public void Analyzer_RejectsImpossibleInMemoryMeasurement()
    {
        var invalid = Entry(
            "impossible",
            size: 1_000,
            status: "completed",
            durationMilliseconds: 100,
            measuredBytes: 1_001,
            DateTimeOffset.UtcNow);

        var summary = TransferPerformanceAnalyzer.Summarize([invalid]);

        Assert.Equal(1, summary.CompletedRecords);
        Assert.Equal(1_000, summary.CompletedBytes);
        Assert.Equal(0, summary.MeasuredTransfers);
        Assert.Equal(0d, summary.AverageBytesPerSecond);
        Assert.Equal(0d, TransferPerformanceAnalyzer.BytesPerSecond(invalid));
    }

    [Fact]
    public void Summarize_SaturatesByteCountersInsteadOfOverflowing()
    {
        var now = DateTimeOffset.UtcNow;
        var summary = TransferPerformanceAnalyzer.Summarize(
        [
            Entry("one", long.MaxValue, "completed", 1_000, long.MaxValue, now),
            Entry("two", 1, "completed", 1_000, 1, now)
        ]);

        Assert.Equal(long.MaxValue, summary.CompletedBytes);
        Assert.Equal(long.MaxValue, summary.MeasuredBytes);
        Assert.True(double.IsFinite(summary.AverageBytesPerSecond));
    }

    private static TransferHistoryEntry Entry(
        string id,
        long size,
        string status,
        long? durationMilliseconds,
        long? measuredBytes,
        DateTimeOffset timestamp)
        => new(id, "sent", "Device", "file.bin", size, timestamp, status, true, durationMilliseconds, measuredBytes);
}
