using SwiftDrop.Core.Models;
using SwiftDrop.Core.Storage;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.App.Services;

public sealed class TransferQueueService
{
    private readonly AppSettingsService _settings;
    private readonly TransferActivityService _activity;
    private readonly TransferNotificationService _notifications;
    private readonly TransferQueueMetadataStore _store;
    private readonly AsyncConcurrencyGate _gate = new();
    private readonly SemaphoreSlim _initializationGate = new(1, 1);
    private readonly object _sync = new();
    private readonly Dictionary<Guid, TransferQueueEntry> _entries = new();
    private bool _initialized;
    private bool _persistenceAvailable = true;

    public TransferQueueService(
        AppSettingsService settings,
        TransferActivityService activity,
        TransferNotificationService notifications)
    {
        _settings = settings;
        _activity = activity;
        _notifications = notifications;
        _store = new TransferQueueMetadataStore(Path.Combine(FileSystem.AppDataDirectory, "swiftdrop.db"));
    }

    public event EventHandler? Changed;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_initialized) return;
        await _initializationGate.WaitAsync(ct);
        try
        {
            if (_initialized) return;
            try
            {
                await _store.InitializeAsync(ct);
                await _store.MarkInFlightInterruptedAsync(DateTimeOffset.UtcNow, ct);
                var persisted = await _store.GetRecentAsync(100, ct);
                lock (_sync)
                {
                    foreach (var row in persisted)
                    {
                        if (!Guid.TryParse(row.Id, out var id) ||
                            !Enum.TryParse<TransferQueueState>(row.State, ignoreCase: false, out var state))
                            continue;
                        _entries[id] = new TransferQueueEntry(
                            id,
                            row.Label,
                            state,
                            row.CreatedUtc,
                            row.StartedUtc,
                            row.FinishedUtc,
                            row.ErrorCode ?? string.Empty,
                            row.ErrorCode);
                    }
                }
            }
            catch
            {
                _persistenceAvailable = false;
            }
            _initialized = true;
        }
        finally
        {
            _initializationGate.Release();
        }
        RaiseChanged();
    }

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
        await InitializeAsync(ct);

        var currentSettings = _settings.Load();
        var visibleLabel = currentSettings.PrivacyMode ? "Transfer" : label;
        var id = Guid.NewGuid();
        var queued = new TransferQueueEntry(id, visibleLabel, TransferQueueState.Queued, DateTimeOffset.UtcNow);
        Update(queued);
        await PersistBestEffortAsync(queued, ct);
        try
        {
            await using var concurrencyLease = await _gate.EnterAsync(currentSettings.TransferConcurrency, ct);
            await using var activityLease = await _activity.EnterAsync(ct);
            var running = Get(id) with { State = TransferQueueState.Running, StartedUtc = DateTimeOffset.UtcNow };
            Update(running);
            await PersistBestEffortAsync(running, ct);

            var result = await action(ct);
            var completed = Get(id) with { State = TransferQueueState.Completed, FinishedUtc = DateTimeOffset.UtcNow };
            Update(completed);
            await PersistBestEffortAsync(completed, CancellationToken.None);
            TrimFinished();
            await TrimPersistenceBestEffortAsync();
            await NotifyBestEffortAsync(success: true);
            return result;
        }
        catch (OperationCanceledException)
        {
            var cancelled = Get(id) with { State = TransferQueueState.Cancelled, FinishedUtc = DateTimeOffset.UtcNow };
            Update(cancelled);
            await PersistBestEffortAsync(cancelled, CancellationToken.None);
            TrimFinished();
            await TrimPersistenceBestEffortAsync();
            throw;
        }
        catch (Exception ex)
        {
            var failed = Get(id) with
            {
                State = TransferQueueState.Failed,
                FinishedUtc = DateTimeOffset.UtcNow,
                Error = SanitizeError(ex, currentSettings.PrivacyMode),
                ErrorCode = ex.GetType().Name
            };
            Update(failed);
            await PersistBestEffortAsync(failed, CancellationToken.None);
            TrimFinished();
            await TrimPersistenceBestEffortAsync();
            await NotifyBestEffortAsync(success: false);
            throw;
        }
    }

    public async Task ClearFinishedAsync(CancellationToken ct = default)
    {
        await InitializeAsync(ct);
        lock (_sync)
        {
            foreach (var id in _entries
                         .Where(x => IsTerminal(x.Value.State))
                         .Select(x => x.Key)
                         .ToArray())
                _entries.Remove(id);
        }
        if (_persistenceAvailable)
        {
            try
            {
                await _store.DeleteFinishedAsync(ct);
            }
            catch
            {
                _persistenceAvailable = false;
            }
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
                .Where(x => IsTerminal(x.State))
                .OrderByDescending(x => x.FinishedUtc)
                .Skip(100)
                .Select(x => x.Id)
                .ToArray();
            foreach (var id in finished) _entries.Remove(id);
        }
        RaiseChanged();
    }

    private async Task PersistBestEffortAsync(TransferQueueEntry entry, CancellationToken ct)
    {
        if (!_persistenceAvailable) return;
        try
        {
            var metadata = new TransferQueueMetadataEntry(
                entry.Id.ToString("N"),
                "Transfer",
                entry.State.ToString(),
                entry.CreatedUtc,
                entry.StartedUtc,
                entry.FinishedUtc,
                entry.ErrorCode);
            await _store.UpsertAsync(metadata, ct);
        }
        catch
        {
            _persistenceAvailable = false;
        }
    }

    private async Task TrimPersistenceBestEffortAsync()
    {
        if (!_persistenceAvailable) return;
        try
        {
            await _store.TrimAsync(100, CancellationToken.None);
        }
        catch
        {
            _persistenceAvailable = false;
        }
    }

    private async Task NotifyBestEffortAsync(bool success)
    {
        try
        {
            if (success) await _notifications.NotifyCompletedAsync();
            else await _notifications.NotifyFailedAsync();
        }
        catch
        {
            // Notification/platform policy must never change the transfer result.
        }
    }

    private void RaiseChanged()
    {
        if (MainThread.IsMainThread) Changed?.Invoke(this, EventArgs.Empty);
        else MainThread.BeginInvokeOnMainThread(() => Changed?.Invoke(this, EventArgs.Empty));
    }

    private static bool IsTerminal(TransferQueueState state)
        => state is TransferQueueState.Completed or TransferQueueState.Failed or TransferQueueState.Cancelled or TransferQueueState.Interrupted;

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
    Cancelled,
    Interrupted
}

public sealed record TransferQueueEntry(
    Guid Id,
    string Label,
    TransferQueueState State,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? StartedUtc = null,
    DateTimeOffset? FinishedUtc = null,
    string? Error = null,
    string? ErrorCode = null);
