using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class AttemptRateLimiterStateMachineTests
{
    [Fact]
    public void DeterministicStateMachine_MatchesReferenceModel()
    {
        const int maxAttempts = 3;
        const int maxKeys = 16;
        var window = TimeSpan.FromSeconds(11);
        var limiter = new AttemptRateLimiter(maxAttempts, window, maxKeys);
        var model = new ReferenceRateLimiter(maxAttempts, window, maxKeys);
        var random = new Random(0x51F7D20);
        var now = new DateTimeOffset(2026, 8, 18, 8, 0, 0, TimeSpan.Zero);

        for (var step = 0; step < 5_000; step++)
        {
            now = now.AddMilliseconds(random.Next(0, 2_501));
            var key = $"peer-{random.Next(0, 24):D2}";

            if (random.Next(100) < 78)
            {
                var expected = model.TryAcquire(key, now);
                var actual = limiter.TryAcquire(key, now);
                Assert.Equal(expected, actual);
            }
            else
            {
                model.Reset(key);
                limiter.Reset(key);
            }
        }
    }

    private sealed class ReferenceRateLimiter
    {
        private readonly Dictionary<string, Queue<DateTimeOffset>> _attempts = new(StringComparer.Ordinal);
        private readonly int _maxAttempts;
        private readonly TimeSpan _window;
        private readonly int _maxKeys;

        public ReferenceRateLimiter(int maxAttempts, TimeSpan window, int maxKeys)
        {
            _maxAttempts = maxAttempts;
            _window = window;
            _maxKeys = maxKeys;
        }

        public bool TryAcquire(string key, DateTimeOffset now)
        {
            if (!_attempts.TryGetValue(key, out var queue))
            {
                if (_attempts.Count >= _maxKeys)
                {
                    PruneExpired(now);
                    if (_attempts.Count >= _maxKeys)
                        return false;
                }

                queue = new Queue<DateTimeOffset>();
                _attempts.Add(key, queue);
            }

            PruneQueue(queue, now);
            if (queue.Count >= _maxAttempts)
                return false;

            queue.Enqueue(now);
            return true;
        }

        public void Reset(string key) => _attempts.Remove(key);

        private void PruneExpired(DateTimeOffset now)
        {
            var expiredKeys = new List<string>();
            foreach (var pair in _attempts)
            {
                PruneQueue(pair.Value, now);
                if (pair.Value.Count == 0)
                    expiredKeys.Add(pair.Key);
            }

            foreach (var key in expiredKeys)
                _attempts.Remove(key);
        }

        private void PruneQueue(Queue<DateTimeOffset> queue, DateTimeOffset now)
        {
            while (queue.Count > 0 && now - queue.Peek() >= _window)
                queue.Dequeue();
        }
    }
}
