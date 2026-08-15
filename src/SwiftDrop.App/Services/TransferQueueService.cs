using SwiftDrop.Core.Models;
using SwiftDrop.Core.Storage;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.App.Services;

public sealed class TransferQueueService
{
    private const int PersistProgressStepBasisPoints = 500;

    private readonly AppSettingsService _settings;
    private readonly TransferActivityService _activity;
    private readonly TransferNotificationService _notifications;
    private readonly TransferQueueMetadataStore _store;
    private readonly AsyncConcurrencyGate _gate = new();
    private readonly SemaphoreSlim _initializationGate = new(1, 1);
    private readonly SemaphoreSlim _persistenceGate = new(1, 1);
    private readonly object _sync = new();
    private readonly Dictionary<Guid, TransferQueueEntry> _entries = new();
    private readonly Dictionary<Guid, int> _lastPersistedProgressBucket = new();
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

                        var operationKind = Enum.TryParse<TransferQueueOperationKind>(row.OperationKind, ignoreCase: false, out var parsedKind)
                            ? parsedKind
                            : TransferQueueOperationKind.Transfer;
                        _entries[id] = new TransferQueueEntry(
                            id,
                            row.Label,
                            state,
                            row.CreatedUtc,
                            row.StartedUtc,
                            row.FinishedUtc,
                            row.ErrorCode,
                            row.ErrorCode,
                            operationKind,
                            row.UpdatedUtc ?? row.CreatedUtc,
                            row.ProgressBasisPoints,
                            row.ItemCount,
                            row.CompletedItemCount);
                        _lastPersistedProgressBucket[id] = ProgressBucket(row.ProgressBasisPoints);
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

    public Task ExecuteAsync(
        string label,
        Func<CancellationToken, Task> action,
        CancellationToken ct = default)
        => ExecuteAsync(
            label,
            TransferQueueOperationKind.Transfer,
            null,
            async (_, token) => await action(token),
            ct);

    public Task ExecuteAsync(
        string label,
        TransferQueueOperationKind operationKind,
        int? itemCount,
        Func<TransferQueueProgressReporter, CancellationToken, Task> action,
        CancellationToken ct = default)
        => ExecuteAsync<object?>(
            label,
            operationKind,
            itemCount,
            async (reporter, token) =>
            {
                await action(reporter, token);
                return null;
            },
            ct);

    public Task<T> ExecuteAsync<T>(
        string label,
        Func<CancellationToken, Task<T>> action,
        CancellationToken ct = default)
        => ExecuteAsync(
            label,
            TransferQueueOperationKind.Transfer,
            null,
            (_, token) => action(token),
            ct);

    public async Task<T> ExecuteAsync<T>(
        string label,
        TransferQueueOperationKind operationKind,
        int? itemCount,
        Func<TransferQueueProgressReporter, CancellationToken, Task<T>> action,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(action);
        if (!Enum.IsDefined(operationKind)) throw new ArgumentOutOfRangeException(nameof(operationKind));
        if (itemCount is < 0) throw new ArgumentOutOfRangeException(nameof(itemCount));
        await InitializeAsync(ct);

        var currentSettings = _settings.Load();
        var visibleLabel = currentSettings.PrivacyMode ? "Transfer" : label;
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var queued = new TransferQueueEntry(
            id,
            visibleLabel,
            TransferQueueState.Queued,
            now,
            OperationKind: operationKind,
            UpdatedUtc: now,
            ItemCount: itemCount,
            CompletedItemCount: itemCount is null ? null : 0);
        Update(queued);
        await PersistCurrentBestEffortAsync(id, ct);

        var progressReporter = new TransferQueueProgressReporter(
            (fraction, completedItems, totalItems) => ReportProgress(id, fraction, completedItems, totalItems));
        try
        {
            await using var concurrencyLease = await _gate.EnterAsync(currentSettings.TransferConcurrency, ct);
            await using var activityLease = await _activity.EnterAsync(ct);
            var runningAt = DateTimeOffset.UtcNow;
            var running = Get(id) with
            {
                State = TransferQueueState.Running,
                StartedUtc = runningAt,
                UpdatedUtc = runningAt
            };
            Update(running);
            await PersistCurrentBestEffortAsync(id, ct);

            var result = await action(progressReporter, ct);
            var completedAt = DateTimeOffset.UtcNow;
            var beforeCompletion = Get(id);
            var completed = beforeCompletion with
            {
                State = TransferQueueState.Completed,
                FinishedUtc = completedAt,
                UpdatedUtc = completedAt,
                ProgressBasisPoints = 10_000,
                CompletedItemCount = beforeCompletion.ItemCount
            };
            Update(completed);
            await PersistCurrentBestEffortAsync(id, CancellationToken.None);
            TrimFinished();
            await TrimPersistenceBestEffortAsync();
            await NotifyBestEffortAsync(success: true);
            return result;
        }
        catch (OperationCanceledException)
        {
            var cancelledAt = DateTimeOffset.UtcNow;
            var cancelled = Get(id) with
            {
                State = TransferQueueState.Cancelled,
                FinishedUtc = cancelledAt,
                UpdatedUtc = cancelledAt
            };
            Update(cancelled);
            await PersistCurrentBestEffortAsync(id, CancellationToken.None);
            TrimFinished();
            await TrimPersistenceBestEffortAsync();
            throw;
        }
        catch (Exception ex)
        {
            var failedAt = DateTimeOffset.UtcNow;
            var failed = Get(id) with
            {
                State = TransferQueueState.Failed,
                FinishedUtc = failedAt,
                UpdatedUtc = failedAt,
                Error = SanitizeError(ex, currentSettings.PrivacyMode),
                ErrorCode = ex.GetType().Name
            };
            Update(failed);
            await PersistCurrentBestEffortAsync(id, CancellationToken.None);
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
            {
                _entries.Remove(id);
                _lastPersistedProgressBucket.Remove(id);
            }
        }
        if (_persistenceAvailable)
        {
            try
            {
                await _persistenceGate.WaitAsync(ct);
                try
                {
                    await _store.DeleteFinishedAsync(ct);
                }
                finally
                {
                    _persistenceGate.Release();
                }
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

    private void ReportProgress(Guid id, double fraction, int? completedItems, int? totalItems)
    {
        if (double.IsNaN(fraction) || double.IsInfinity(fraction)) return;
        var progressBasisPoints = (int)Math.Round(
            Math.Clamp(fraction, 0d, 1d) * 10_000d,
            MidpointRounding.AwayFromZero);

        TransferQueueEntry? updated = null;
        var shouldPersist = false;
        lock (_sync)
        {
            if (!_entries.TryGetValue(id, out var current) || IsTerminal(current.State)) return;

            var normalizedTotal = totalItems is < 0 ? current.ItemCount : totalItems ?? current.ItemCount;
            var normalizedCompleted = completedItems is < 0 ? current.CompletedItemCount : completedItems ?? current.CompletedItemCount;
            if (normalizedTotal is { } total && normalizedCompleted is { } completed && completed > total)
                normalizedCompleted = total;

            var nextProgress = Math.Max(current.ProgressBasisPoints, progressBasisPoints);
            updated = current with
            {
                ProgressBasisPoints = nextProgress,
                ItemCount = normalizedTotal,
                CompletedItemCount = normalizedCompleted,
                UpdatedUtc = DateTimeOffset.UtcNow
            };
            _entries[id] = updated;

            var bucket = ProgressBucket(nextProgress);
            var previousBucket = _lastPersistedProgressBucket.GetValueOrDefault(id, -1);
            shouldPersist = bucket > previousBucket || normalizedCompleted != current.CompletedItemCount || normalizedTotal != current.ItemCount;
            if (shouldPersist) _lastPersistedProgressBucket[id] = bucket;
        }

        RaiseChanged();
        if (shouldPersist && updated is not null)
            _ = PersistCurrentBestEffortAsync(id, CancellationToken.None);
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
            foreach (var id in finished)
            {
                _entries.Remove(id);
                _lastPersistedProgressBucket.Remove(id);
            }
        }
        RaiseChanged();
    }

    private async Task PersistCurrentBestEffortAsync(Guid id, CancellationToken ct)
    {
        if (!_persistenceAvailable) return;
        try
        {
            await _persistenceGate.WaitAsync(ct);
            try
            {
                TransferQueueEntry entry;
                lock (_sync)
                {
                    if (!_entries.TryGetValue(id, out entry!)) return;
                }

                var metadata = new TransferQueueMetadataEntry(
                    entry.Id.ToString("N"),
                    "Transfer",
                    entry.State.ToString(),
                    entry.CreatedUtc,
                    entry.StartedUtc,
                    entry.FinishedUtc,
                    entry.ErrorCode,
                    entry.OperationKind.ToString(),
                    entry.UpdatedUtc ?? entry.CreatedUtc,
                    entry.ProgressBasisPoints,
                    entry.ItemCount,
                    entry.CompletedItemCount);
                await _store.UpsertAsync(metadata, ct);
                lock (_sync) _lastPersistedProgressBucket[id] = ProgressBucket(entry.ProgressBasisPoints);
            }
            finally
            {
                _persistenceGate.Release();
            }
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
            await _persistenceGate.WaitAsync(CancellationToken.None);
            try
            {
                await _store.TrimAsync(100, CancellationToken.None);
            }
            finally
            {
                _persistenceGate.Release();
            }
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

    private static int ProgressBucket(int progressBasisPoints)
        => progressBasisPoints / PersistProgressStepBasisPoints;

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

public enum TransferQueueOperationKind
{
    Transfer,
    File,
    Batch,
    Text,
    Receive
}

public sealed class TransferQueueProgressReporter
{
    private readonly Action<double, int?, int?> _report;

    internal TransferQueueProgressReporter(Action<double, int?, int?> report)
    {
        _report = report;
    }

    public void Report(double fraction, int? completedItems = null, int? totalItems = null)
        => _report(fraction, completedItems, totalItems);
}

public sealed record TransferQueueEntry(
    Guid Id,
    string Label,
    TransferQueueState State,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? StartedUtc = null,
    DateTimeOffset? FinishedUtc = null,
    string? Error = null,
    string? ErrorCode = null,
    TransferQueueOperationKind OperationKind = TransferQueueOperationKind.Transfer,
    DateTimeOffset? UpdatedUtc = null,
    int ProgressBasisPoints = 0,
    int? ItemCount = null,
    int? CompletedItemCount = null)
{
    public double ProgressFraction => ProgressBasisPoints / 10_000d;
}
