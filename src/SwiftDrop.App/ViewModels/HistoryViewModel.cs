using System.Collections.ObjectModel;
using SwiftDrop.App.Services;
using SwiftDrop.Core.Models;

namespace SwiftDrop.App.ViewModels;

public sealed class HistoryViewModel : ObservableObject
{
    private readonly TransferHistoryService _history;
    private bool _isBusy;
    private string _status = string.Empty;

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
    }

    public sealed record HistoryRow(
        string Id,
        string FileNameText,
        string PeerDeviceNameText,
        string DirectionText,
        string SizeText,
        string StatusText,
        string TimestampText,
        bool IntegrityVerified)
    {
        public static HistoryRow FromEntry(TransferHistoryEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            return new HistoryRow(
                entry.Id,
                PresentSensitive(entry.FileName),
                PresentSensitive(entry.PeerDeviceName),
                LocalizeDirection(entry.Direction),
                AppText.Format("HistoryBytesFormat", entry.SizeBytes),
                LocalizeStatus(entry.Status),
                AppText.Format("HistoryTimeFormat", entry.TimestampUtc.LocalDateTime),
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
