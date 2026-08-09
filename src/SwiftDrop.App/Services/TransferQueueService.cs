using SwiftDrop.Core.Transfer;

namespace SwiftDrop.App.Services;

public sealed class TransferQueueService
{
    private readonly AppSettingsService _settings;
    private readonly AsyncConcurrencyGate _gate = new();
    private readonly object _sync = new();
    private readonly Dictionary<Guid, TransferQueueEntry> _entries = new();

    public TransferQueueService(AppSettingsService settings)
    {
        _settings = settings;
    }

    public event EventHandler? Changed;

    public IReadOnlyList<TransferQueueEntry> Snapshot()
    {
        lock (_sync)
            return _entries.Values.OrderByDescending(x => x.CreatedUtc).ToArray();
    }

    public async Task ExecuteAsync(
        string label,
        Func<CancellationToken, Task> action,
        CancellationToken ct = default)
    {
        await ExecuteAsync<object?>(
            label,
            async token =>
            {
                await action(token);
                return null;
            },
            ct);
    }

    public async Task<T> ExecuteAsync<T>(
        string label,
        Func<CancellationToken, Task<T>> action,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(action);

        var currentSettings = _settings.Load();
        var visibleLabel = currentSettings.PrivacyMode ? "Transfer" : label;
        var id = Guid.NewGuid();
        Update(new TransferQueueEntry(id, visibleLabel, TransferQueueState.Queued, DateTimeOffset.UtcNow));
        try
        {
            await using var lease = await _gate.EnterAsync(currentSettings.TransferConcurrency, ct);
            Update(Get(id) with { State = TransferQueueState.Running, StartedUtc = DateTimeOffset.UtcNow });
            var result = await action(ct);
            Update(Get(id) with { State = TransferQueueState.Completed, FinishedUtc = DateTimeOffset.UtcNow });
            TrimFinished();
            return result;
        }
        catch (OperationCanceledException)
        {
            Update(Get(id) with { State = TransferQueueState.Cancelled, FinishedUtc = DateTimeOffset.UtcNow });
            TrimFinished();
            throw;
        }
        catch (Exception ex)
        {
            Update(Get(id) with
            {
                State = TransferQueueState.Failed,
                FinishedUtc = DateTimeOffset.UtcNow,
                Error = SanitizeError(ex, currentSettings.PrivacyMode)
            });
            TrimFinished();
            throw;
        }
    }

    public void ClearFinished()
    {
        lock (_sync)
        {
            foreach (var id in _entries.Where(x => x.Value.State is TransferQueueState.Completed or TransferQueueState.Cancelled or TransferQueueState.Failed).Select(x => x.Key).ToArray())
                _entries.Remove(id);
        }
        RaiseChanged();
    }

    private TransferQueueEntry Get(Guid id)
    {
        lock (_sync) return _entries[id];
    }

    private void Update(TransferQueueEntry entry)
    {
        lock (_sync) _entries[entry.Id] = entry;
        RaiseChanged();
    }

    private void TrimFinished()
    {
        lock (_sync)
        {
            var finished = _entries.Values
                .Where(x => x.State is TransferQueueState.Completed or TransferQueueState.Cancelled or TransferQueueState.Failed)
                .OrderByDescending(x => x.FinishedUtc)
                .Skip(100)
                .Select(x => x.Id)
                .ToArray();
            foreach (var id in finished) _entries.Remove(id);
        }
        RaiseChanged();
    }

    private void RaiseChanged()
    {
        if (MainThread.IsMainThread) Changed?.Invoke(this, EventArgs.Empty);
        else MainThread.BeginInvokeOnMainThread(() => Changed?.Invoke(this, EventArgs.Empty));
    }

    private static string SanitizeError(Exception ex, bool privacyMode)
    {
        if (privacyMode) return ex.GetType().Name;
        var message = ex.Message.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return message.Length <= 240 ? message : message[..240];
    }
}

public enum TransferQueueState
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled
}

public sealed record TransferQueueEntry(
    Guid Id,
    string Label,
    TransferQueueState State,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? StartedUtc = null,
    DateTimeOffset? FinishedUtc = null,
    string? Error = null);
