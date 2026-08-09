using System.Collections.Concurrent;

namespace SwiftDrop.Core.Security;

public sealed class AttemptRateLimiter
{
    private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> _attempts = new(StringComparer.Ordinal);
    private readonly int _maxAttempts;
    private readonly TimeSpan _window;

    public AttemptRateLimiter(int maxAttempts, TimeSpan window)
    {
        if (maxAttempts < 1) throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        if (window <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(window));
        _maxAttempts = maxAttempts;
        _window = window;
    }

    public bool TryAcquire(string key, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var queue = _attempts.GetOrAdd(key, static _ => new Queue<DateTimeOffset>());
        lock (queue)
        {
            while (queue.Count > 0 && now - queue.Peek() >= _window)
                queue.Dequeue();
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
}
