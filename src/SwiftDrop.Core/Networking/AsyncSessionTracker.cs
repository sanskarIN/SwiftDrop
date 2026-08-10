using System.Collections.Concurrent;

namespace SwiftDrop.Core.Networking;

public sealed class AsyncSessionTracker
{
    private readonly ConcurrentDictionary<long, Task> _active = new();
    private long _nextId;

    public int Count => _active.Count;

    public void Track(Task session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var id = Interlocked.Increment(ref _nextId);
        if (!_active.TryAdd(id, session))
            throw new InvalidOperationException("Unable to register active session.");
        _ = ObserveAsync(id, session);
    }

    public async Task DrainAsync(CancellationToken ct = default)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var snapshot = _active.Values.ToArray();
            if (snapshot.Length == 0) return;

            try
            {
                await Task.WhenAll(snapshot).WaitAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                // A faulted session is still drained; the session owner handles/logs its failure.
            }

            if (_active.IsEmpty) return;
        }
    }

    private async Task ObserveAsync(long id, Task session)
    {
        try
        {
            await session;
        }
        catch
        {
        }
        finally
        {
            _active.TryRemove(id, out _);
        }
    }
}
