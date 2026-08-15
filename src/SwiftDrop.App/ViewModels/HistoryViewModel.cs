using System.Collections.ObjectModel;
using SwiftDrop.App.Services;
using SwiftDrop.Core.Diagnostics;
using SwiftDrop.Core.Models;

namespace SwiftDrop.App.ViewModels;

public sealed class HistoryViewModel : ObservableObject
{
    private readonly TransferHistoryService _history;
    private bool _isBusy;
    private string _status = string.Empty;
    private string _performanceStatus = string.Empty;

    public HistoryViewModel(TransferHistoryService history)
    {
        _history = history;
    }

    public ObservableCollection<HistoryRow> Items { get; } = new();

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public string PerformanceStatus
    {
        get => _performanceStatus;
        private set => SetProperty(ref _performanceStatus, value);
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await _history.InitializeAsync(ct);
            var items = await _history.GetRecentAsync(200, ct);
            Items.Clear();
            foreach (var item in items) Items.Add(HistoryRow.FromEntry(item));
            Status = Items.Count switch
            {
                0 => AppText.Get("NoHistoryRecords"),
                1 => AppText.Get("HistoryCountOne"),
                _ => AppText.Format("HistoryCountFormat", Items.Count)
            };
            PerformanceStatus = FormatPerformanceSummary(TransferPerformanceAnalyzer.Summarize(items));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        await _history.DeleteAsync(id, ct);
        await RefreshAsync(ct);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        await _history.ClearAsync(ct);
        Items.Clear();
        Status = AppText.Get("NoHistoryRecords");
        PerformanceStatus = AppText.Get("HistoryPerformanceNoMeasurements");
    }

    private static string FormatPerformanceSummary(TransferPerformanceSummary summary)
    {
        if (!summary.HasMeasurements)
            return AppText.Get("HistoryPerformanceNoMeasurements");

        return AppText.Format(
            "HistoryPerformanceSummaryFormat",
            summary.CompletedRecords,
            FormatBytes(summary.CompletedBytes),
            summary.MeasuredTransfers,
            FormatBytes(ToDisplayRate(summary.AverageBytesPerSecond)));
    }

    private static long ToDisplayRate(double bytesPerSecond)
    {
        if (!double.IsFinite(bytesPerSecond) || bytesPerSecond <= 0d) return 0;
        return bytesPerSecond >= long.MaxValue ? long.MaxValue : (long)Math.Round(bytesPerSecond);
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];
        double value = Math.Max(0, bytes);
        var unit = 0;
        while (value >= 1024d && unit < units.Length - 1)
        {
            value /= 1024d;
            unit++;
        }
        return $"{value:0.##} {units[unit]}";
    }

    public sealed record HistoryRow(
        string Id,
        string FileNameText,
        string PeerDeviceNameText,
        string DirectionText,
        string SizeText,
        string StatusText,
        string TimestampText,
        string DurationText,
        string ThroughputText,
        bool IntegrityVerified)
    {
        public static HistoryRow FromEntry(TransferHistoryEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            var bytesPerSecond = TransferPerformanceAnalyzer.BytesPerSecond(entry);
            var hasMeasurement = bytesPerSecond > 0d && entry.DurationMilliseconds is > 0;
            return new HistoryRow(
                entry.Id,
                PresentSensitive(entry.FileName),
                PresentSensitive(entry.PeerDeviceName),
                LocalizeDirection(entry.Direction),
                AppText.Format("HistoryBytesFormat", entry.SizeBytes),
                LocalizeStatus(entry.Status),
                AppText.Format("HistoryTimeFormat", entry.TimestampUtc.LocalDateTime),
                hasMeasurement
                    ? AppText.Format("HistoryDurationFormat", entry.DurationMilliseconds!.Value / 1000d)
                    : string.Empty,
                hasMeasurement
                    ? AppText.Format("HistoryThroughputFormat", FormatBytes(ToDisplayRate(bytesPerSecond)))
                    : string.Empty,
                entry.IntegrityVerified);
        }

        private static string PresentSensitive(string value)
            => string.Equals(value, TransferHistoryService.PrivacyRedactionMarker, StringComparison.Ordinal) ||
               string.Equals(value, "Hidden by privacy mode", StringComparison.Ordinal)
                ? AppText.Get("PrivacyHidden")
                : value;

        private static string LocalizeDirection(string direction)
            => direction switch
            {
                "sent" => AppText.Get("HistorySent"),
                "received" => AppText.Get("HistoryReceived"),
                _ => direction
            };

        private static string LocalizeStatus(string status)
            => status switch
            {
                "completed" => AppText.Get("HistoryCompleted"),
                "failed" => AppText.Get("HistoryFailed"),
                "cancelled" => AppText.Get("HistoryCancelled"),
                "paused" => AppText.Get("HistoryPaused"),
                "rejected" => AppText.Get("HistoryRejected"),
                "not-selected" => AppText.Get("HistoryNotSelected"),
                "accepted" => AppText.Get("HistoryAccepted"),
                "copied" => AppText.Get("HistoryCopied"),
                _ => status
            };
    }
}
