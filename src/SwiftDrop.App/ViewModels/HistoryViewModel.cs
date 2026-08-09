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

    public ObservableCollection<TransferHistoryEntry> Items { get; } = new();

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
            foreach (var item in items) Items.Add(item);
            Status = Items.Count == 1 ? "1 local history record" : $"{Items.Count:N0} local history records";
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
        Status = "No local history records";
    }
}
