using SwiftDrop.Core.Diagnostics;
using SwiftDrop.Core.Models;

namespace SwiftDrop.Core.Tests;

public sealed class TransferPerformanceTrendAnalyzerTests
{
    [Fact]
    public void BuildDaily_GroupsValidMeasurementsByUtcDate()
    {
        TransferHistoryEntry[] rows =
        [
            Entry("a", new DateTimeOffset(2026, 8, 14, 23, 30, 0, TimeSpan.Zero), 4_000, 1_000, 4_000),
            Entry("b", new DateTimeOffset(2026, 8, 15, 0, 15, 0, TimeSpan.Zero), 2_000, 500, 2_000),
            Entry("c", new DateTimeOffset(2026, 8, 15, 1, 0, 0, TimeSpan.Zero), 6_000, 1_500, 6_000)
        ];

        var points = TransferPerformanceTrendAnalyzer.BuildDaily(
            rows,
            new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero),
            2);

        Assert.Equal(2, points.Count);
        Assert.Equal(new DateOnly(2026, 8, 14), points[0].DateUtc);
        Assert.Equal(1, points[0].MeasuredTransfers);
        Assert.Equal(4_000, points[0].MeasuredBytes);
        Assert.Equal(4_000d, points[0].AverageBytesPerSecond, 6);
        Assert.Equal(new DateOnly(2026, 8, 15), points[1].DateUtc);
        Assert.Equal(2, points[1].MeasuredTransfers);
        Assert.Equal(8_000, points[1].MeasuredBytes);
        Assert.Equal(2_000, points[1].MeasuredDurationMilliseconds);
        Assert.Equal(4_000d, points[1].AverageBytesPerSecond, 6);
    }

    [Fact]
    public void BuildDaily_UsesActualMeasuredBytesForResumedTransfers()
    {
        var row = Entry(
            "resumed",
            new DateTimeOffset(2026, 8, 15, 2, 0, 0, TimeSpan.Zero),
            logicalSize: 10_000,
            durationMilliseconds: 1_000,
            measuredBytes: 2_500);

        var point = Assert.Single(TransferPerformanceTrendAnalyzer.BuildDaily(
            [row],
            new DateTimeOffset(2026, 8, 15, 23, 0, 0, TimeSpan.Zero),
            1));

        Assert.Equal(2_500, point.MeasuredBytes);
        Assert.Equal(2_500d, point.AverageBytesPerSecond, 6);
    }

    [Fact]
    public void BuildDaily_UsesUtcCalendarDateAcrossOffsetInputs()
    {
        var row = Entry(
            "offset",
            new DateTimeOffset(2026, 8, 16, 4, 45, 0, TimeSpan.FromHours(5.5)),
            1_000,
            1_000,
            1_000);

        var point = Assert.Single(TransferPerformanceTrendAnalyzer.BuildDaily(
            [row],
            new DateTimeOffset(2026, 8, 15, 23, 59, 0, TimeSpan.Zero),
            1));

        Assert.Equal(new DateOnly(2026, 8, 15), point.DateUtc);
    }

    [Fact]
    public void BuildDaily_ExcludesRowsOutsideWindowAndInvalidMeasurements()
    {
        var end = new DateTimeOffset(2026, 8, 15, 23, 59, 0, TimeSpan.Zero);
        TransferHistoryEntry[] rows =
        [
            Entry("inside", end.AddHours(-1), 1_000, 500, 1_000),
            Entry("old", end.AddDays(-2), 1_000, 500, 1_000),
            Entry("future", end.AddDays(1), 1_000, 500, 1_000),
            Entry("failed", end.AddHours(-2), 1_000, 500, 1_000, status: "failed"),
            Entry("legacy", end.AddHours(-3), 1_000, null, null)
        ];

        var point = Assert.Single(TransferPerformanceTrendAnalyzer.BuildDaily(rows, end, 2));

        Assert.Equal(1, point.MeasuredTransfers);
        Assert.Equal(1_000, point.MeasuredBytes);
    }

    [Fact]
    public void BuildDaily_SaturatesAggregateCountersInsteadOfOverflowing()
    {
        var timestamp = new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);
        var point = Assert.Single(TransferPerformanceTrendAnalyzer.BuildDaily(
        [
            Entry("max", timestamp, long.MaxValue, 1_000, long.MaxValue),
            Entry("extra", timestamp, 1, 1_000, 1)
        ], timestamp, 1));

        Assert.Equal(long.MaxValue, point.MeasuredBytes);
        Assert.Equal(2_000, point.MeasuredDurationMilliseconds);
        Assert.True(double.IsFinite(point.AverageBytesPerSecond));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3651)]
    public void BuildDaily_RejectsInvalidWindowLength(int windowDays)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TransferPerformanceTrendAnalyzer.BuildDaily(
                Array.Empty<TransferHistoryEntry>(),
                DateTimeOffset.UtcNow,
                windowDays));
    }

    [Fact]
    public void BuildDaily_RejectsPreUnixWindowEnd()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TransferPerformanceTrendAnalyzer.BuildDaily(
                Array.Empty<TransferHistoryEntry>(),
                DateTimeOffset.UnixEpoch.AddTicks(-1),
                1));
    }

    private static TransferHistoryEntry Entry(
        string id,
        DateTimeOffset timestamp,
        long logicalSize,
        long? durationMilliseconds,
        long? measuredBytes,
        string status = "completed")
        => new(
            id,
            "sent",
            "Device",
            "file.bin",
            logicalSize,
            timestamp,
            status,
            integrityVerified: true,
            durationMilliseconds,
            measuredBytes);
}
