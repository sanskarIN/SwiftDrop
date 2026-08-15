using System.Collections.Concurrent;

namespace SwiftDrop.Core.Security;

public sealed class AttemptRateLimiter
{
    private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> _attempts = new(StringComparer.Ordinal);
    private readonly object _cleanupGate = new();
    private readonly int _maxAttempts;
    private readonly TimeSpan _window;
    private readonly int _maxKeys;

    public AttemptRateLimiter(int maxAttempts, TimeSpan window, int maxKeys = 4096)
    {
        if (maxAttempts < 1) throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        if (window <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(window));
        if (maxKeys < 16) throw new ArgumentOutOfRangeException(nameof(maxKeys));
        _maxAttempts = maxAttempts;
        _window = window;
        _maxKeys = maxKeys;
    }

    public bool TryAcquire(string key, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (key.Length > 256) return false;

        if (!_attempts.TryGetValue(key, out var queue))
        {
            lock (_cleanupGate)
            {
                if (!_attempts.TryGetValue(key, out queue))
                {
                    if (_attempts.Count >= _maxKeys)
                    {
                        PruneExpired(now);
                        if (_attempts.Count >= _maxKeys)
                            return false;
                    }

                    queue = new Queue<DateTimeOffset>();
                    if (!_attempts.TryAdd(key, queue))
                        queue = _attempts[key];
                }
            }
        }

        lock (queue)
        {
            PruneQueue(queue, now);
            if (queue.Count >= _maxAttempts) return false;
            queue.Enqueue(now);
            return true;
        }
    }

    public void Reset(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _attempts.TryRemove(key, out _);
    }

    private void PruneExpired(DateTimeOffset now)
    {
        foreach (var item in _attempts)
        {
            var queue = item.Value;
            var empty = false;
            lock (queue)
            {
                PruneQueue(queue, now);
                empty = queue.Count == 0;
            }
            if (empty)
                _attempts.TryRemove(new KeyValuePair<string, Queue<DateTimeOffset>>(item.Key, queue));
        }
    }

    private void PruneQueue(Queue<DateTimeOffset> queue, DateTimeOffset now)
    {
        while (queue.Count > 0 && now - queue.Peek() >= _window)
            queue.Dequeue();
    }
}
