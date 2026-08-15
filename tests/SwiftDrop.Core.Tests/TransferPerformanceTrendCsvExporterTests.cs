using System.Globalization;
using SwiftDrop.Core.Diagnostics;

namespace SwiftDrop.Core.Tests;

public sealed class TransferPerformanceTrendCsvExporterTests
{
    [Fact]
    public void Export_UsesDeterministicInvariantUtcCsv()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("hi-IN");
            var csv = TransferPerformanceTrendCsvExporter.Export(
            [
                Point(new DateOnly(2026, 8, 15), 2, 8_000, 2_000),
                Point(new DateOnly(2026, 8, 14), 1, 1_500, 1_000)
            ]);

            Assert.Equal(
                TransferPerformanceTrendCsvExporter.Header + "\r\n" +
                "2026-08-14,1,1500,1000,1500\r\n" +
                "2026-08-15,2,8000,2000,4000\r\n",
                csv);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Export_EmptyTrendContainsHeaderOnly()
    {
        Assert.Equal(
            TransferPerformanceTrendCsvExporter.Header + "\r\n",
            TransferPerformanceTrendCsvExporter.Export(Array.Empty<TransferPerformanceTrendPoint>()));
    }

    [Fact]
    public void Export_ContainsOnlyAggregateColumns()
    {
        var csv = TransferPerformanceTrendCsvExporter.Export(
            [Point(new DateOnly(2026, 8, 15), 1, 1_000, 1_000)]);

        Assert.DoesNotContain("file", csv, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("peer", csv, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("device", csv, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("direction", csv, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("path", csv, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", csv, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("nonce", csv, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("certificate", csv, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Export_RejectsDuplicateUtcDates()
    {
        var date = new DateOnly(2026, 8, 15);
        Assert.Throws<ArgumentException>(() => TransferPerformanceTrendCsvExporter.Export(
        [
            Point(date, 1, 1_000, 1_000),
            Point(date, 2, 2_000, 1_000)
        ]));
    }

    [Fact]
    public void Export_RejectsInconsistentThroughput()
    {
        var invalid = new TransferPerformanceTrendPoint(
            new DateOnly(2026, 8, 15),
            1,
            1_000,
            1_000,
            999d);

        Assert.Throws<ArgumentException>(() => TransferPerformanceTrendCsvExporter.Export([invalid]));
    }

    [Theory]
    [InlineData(0, 1_000, 1_000)]
    [InlineData(1, 0, 1_000)]
    [InlineData(1, 1_000, 0)]
    public void Export_RejectsNonPositiveAggregateMeasurements(int transfers, long bytes, long duration)
    {
        var rate = duration > 0 ? bytes * 1000d / duration : 0d;
        var invalid = new TransferPerformanceTrendPoint(
            new DateOnly(2026, 8, 15),
            transfers,
            bytes,
            duration,
            rate);

        Assert.Throws<ArgumentException>(() => TransferPerformanceTrendCsvExporter.Export([invalid]));
    }

    private static TransferPerformanceTrendPoint Point(
        DateOnly date,
        int transfers,
        long bytes,
        long durationMilliseconds)
        => new(
            date,
            transfers,
            bytes,
            durationMilliseconds,
            bytes * 1000d / durationMilliseconds);
}
