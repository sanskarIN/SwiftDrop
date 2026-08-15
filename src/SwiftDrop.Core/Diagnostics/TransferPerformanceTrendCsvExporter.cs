using System.Globalization;
using System.Text;

namespace SwiftDrop.Core.Diagnostics;

public static class TransferPerformanceTrendCsvExporter
{
    public const string Header = "date_utc,measured_transfers,measured_bytes,measured_duration_ms,weighted_bytes_per_second";

    public static string Export(IEnumerable<TransferPerformanceTrendPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        var ordered = points
            .Select(point => point ?? throw new ArgumentException("Trend points cannot contain null values.", nameof(points)))
            .OrderBy(point => point.DateUtc)
            .ToArray();

        var seenDates = new HashSet<DateOnly>();
        var builder = new StringBuilder();
        builder.Append(Header).Append("\r\n");

        foreach (var point in ordered)
        {
            ValidatePoint(point);
            if (!seenDates.Add(point.DateUtc))
                throw new ArgumentException("Trend points must contain at most one bucket per UTC date.", nameof(points));

            builder.Append(point.DateUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(',')
                .Append(point.MeasuredTransfers.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(point.MeasuredBytes.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(point.MeasuredDurationMilliseconds.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(point.AverageBytesPerSecond.ToString("R", CultureInfo.InvariantCulture))
                .Append("\r\n");
        }

        return builder.ToString();
    }

    private static void ValidatePoint(TransferPerformanceTrendPoint point)
    {
        if (!point.HasMeasurements)
            throw new ArgumentException("Trend points must contain positive measured transfer data.", nameof(point));

        var expectedRate = point.MeasuredBytes * 1000d / point.MeasuredDurationMilliseconds;
        var tolerance = Math.Max(1e-9, Math.Abs(expectedRate) * 1e-12);
        if (Math.Abs(point.AverageBytesPerSecond - expectedRate) > tolerance)
            throw new ArgumentException("Trend point throughput must match measured bytes and duration.", nameof(point));
    }
}
