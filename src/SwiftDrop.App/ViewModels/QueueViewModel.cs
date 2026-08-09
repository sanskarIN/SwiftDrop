using System.Collections.ObjectModel;
using SwiftDrop.App.Services;

namespace SwiftDrop.App.ViewModels;

public sealed class QueueViewModel : ObservableObject, IDisposable
{
    private readonly TransferQueueService _queue;
    private string _status = string.Empty;

    public QueueViewModel(TransferQueueService queue)
    {
        _queue = queue;
        _queue.Changed += QueueChanged;
    }

    public ObservableCollection<QueueRow> Items { get; } = new();

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await _queue.InitializeAsync(ct);
        Refresh();
    }

    public void Refresh()
    {
        var rows = _queue.Snapshot().Select(QueueRow.FromEntry).ToArray();
        Items.Clear();
        foreach (var row in rows) Items.Add(row);
        var running = rows.Count(x => x.State == TransferQueueState.Running.ToString());
        var queued = rows.Count(x => x.State == TransferQueueState.Queued.ToString());
        var interrupted = rows.Count(x => x.State == TransferQueueState.Interrupted.ToString());
        Status = $"{running:N0} running • {queued:N0} queued • {interrupted:N0} interrupted";
    }

    public async Task ClearFinishedAsync(CancellationToken ct = default)
    {
        await _queue.ClearFinishedAsync(ct);
        Refresh();
    }

    private void QueueChanged(object? sender, EventArgs e) => Refresh();

    public void Dispose() => _queue.Changed -= QueueChanged;

    public sealed record QueueRow(string Label, string State, string TimingText, string Error)
    {
        public static QueueRow FromEntry(TransferQueueEntry entry)
        {
            var timing = entry.State switch
            {
                TransferQueueState.Queued => $"Queued {entry.CreatedUtc.LocalDateTime:T}",
                TransferQueueState.Running => $"Started {entry.StartedUtc?.LocalDateTime:T}",
                TransferQueueState.Interrupted => $"Interrupted {entry.FinishedUtc?.LocalDateTime:T}",
                _ => $"Finished {entry.FinishedUtc?.LocalDateTime:T}"
            };
            return new QueueRow(entry.Label, entry.State.ToString(), timing, entry.Error ?? string.Empty);
        }
    }
}
