using SwiftDrop.App.Services;

namespace SwiftDrop.App;

public partial class QueuePage : ContentPage
{
    private readonly TransferQueueService _queue;

    public QueuePage(TransferQueueService queue)
    {
        InitializeComponent();
        _queue = queue;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        _queue.Changed += QueueChanged;
        await _queue.InitializeAsync();
        Refresh();
    }

    private void OnUnloaded(object? sender, EventArgs e)
        => _queue.Changed -= QueueChanged;

    private void QueueChanged(object? sender, EventArgs e) => Refresh();
    private void RefreshClicked(object? sender, EventArgs e) => Refresh();

    private async void ClearFinishedClicked(object? sender, EventArgs e)
    {
        await _queue.ClearFinishedAsync();
        Refresh();
    }

    private void Refresh()
    {
        QueueList.ItemsSource = _queue.Snapshot().Select(QueueRow.FromEntry).ToArray();
    }

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
