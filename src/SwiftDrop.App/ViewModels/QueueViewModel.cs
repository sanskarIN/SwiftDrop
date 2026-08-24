using System.Collections.ObjectModel;
using SwiftDrop.App.Services;

namespace SwiftDrop.App.ViewModels;

public sealed class QueueViewModel : ObservableObject, IDisposable
{
    private readonly TransferQueueService _queue;
    private string _status = string.Empty;
    private bool _subscribed;

    public QueueViewModel(TransferQueueService queue)
    {
        _queue = queue;
        Subscribe();
    }

    public ObservableCollection<QueueRow> Items { get; } = new();

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        Subscribe();
        await _queue.InitializeAsync(ct);
        Refresh();
    }

    public void Refresh()
    {
        var entries = _queue.Snapshot();
        var rows = entries.Select(QueueRow.FromEntry).ToArray();
        Items.Clear();
        foreach (var row in rows) Items.Add(row);
        var running = entries.Count(x => x.State == TransferQueueState.Running);
        var queued = entries.Count(x => x.State == TransferQueueState.Queued);
        var interrupted = entries.Count(x => x.State == TransferQueueState.Interrupted);
        Status = LocalizedStatusFormatter.QueueCounts(running, queued, interrupted);
    }

    public async Task ClearFinishedAsync(CancellationToken ct = default)
    {
        await _queue.ClearFinishedAsync(ct);
        Refresh();
    }

    private void Subscribe()
    {
        if (_subscribed) return;
        _queue.Changed += QueueChanged;
        _subscribed = true;
    }

    private void QueueChanged(object? sender, EventArgs e) => Refresh();

    public void Dispose()
    {
        if (!_subscribed) return;
        _queue.Changed -= QueueChanged;
        _subscribed = false;
    }

    public sealed record QueueRow(
        string Label,
        string OperationKind,
        string State,
        double ProgressFraction,
        string ProgressText,
        string TimingText,
        string Error)
    {
        public static QueueRow FromEntry(TransferQueueEntry entry)
        {
            var timestamp = entry.State switch
            {
                TransferQueueState.Queued => entry.CreatedUtc,
                TransferQueueState.Running => entry.StartedUtc ?? entry.CreatedUtc,
                _ => entry.FinishedUtc ?? entry.CreatedUtc
            };
            var timing = LocalizedStatusFormatter.QueueTiming(entry.State, timestamp);

            var progress = entry.ProgressFraction.ToString("P0", System.Globalization.CultureInfo.CurrentCulture);
            if (entry.ItemCount is { } total && entry.CompletedItemCount is { } completed)
                progress = $"{progress} · {completed:N0}/{total:N0}";

            return new QueueRow(
                entry.Label,
                LocalizedStatusFormatter.QueueOperation(entry.OperationKind),
                LocalizedStatusFormatter.QueueState(entry.State),
                entry.ProgressFraction,
                progress,
                timing,
                entry.Error ?? string.Empty);
        }
    }
}
