using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class OneTimeAuthorizationStoreStateMachineTests
{
    [Fact]
    public void DeterministicStateMachine_MatchesReferenceModel()
    {
        const int capacity = 8;
        var store = new OneTimeAuthorizationStore(capacity);
        var model = new ReferenceAuthorizationStore(capacity);
        var random = new Random(0x7A11C0DE);
        var now = new DateTimeOffset(2026, 8, 18, 9, 0, 0, TimeSpan.Zero);

        for (var step = 0; step < 4_000; step++)
        {
            now = now.AddMilliseconds(random.Next(0, 3_001));
            var nonce = Nonce(random.Next(0, 14));
            var operation = random.Next(100);

            if (operation < 52)
            {
                var expires = random.Next(100) < 14
                    ? now
                    : now.AddMilliseconds(random.Next(1, 10_001));

                var expected = model.Register(nonce, expires, now);
                var actual = CaptureRegisterResult(store, nonce, expires, now);
                Assert.Equal(expected, actual);
            }
            else if (operation < 79)
            {
                Assert.Equal(model.TryConsume(nonce, now), store.TryConsume(nonce, now));
            }
            else if (operation < 94)
            {
                Assert.Equal(model.PruneExpired(now), store.PruneExpired(now));
            }
            else
            {
                model.Clear();
                store.Clear();
            }

            Assert.Equal(model.Count, store.Count);
        }
    }

    private static RegisterResult CaptureRegisterResult(
        OneTimeAuthorizationStore store,
        string nonce,
        DateTimeOffset expires,
        DateTimeOffset now)
    {
        try
        {
            store.Register(nonce, expires, now);
            return RegisterResult.Added;
        }
        catch (ArgumentOutOfRangeException)
        {
            return RegisterResult.Expired;
        }
        catch (InvalidOperationException)
        {
            return RegisterResult.Rejected;
        }
    }

    private static string Nonce(int index) => $"auth_{index:D12}_token";

    private enum RegisterResult
    {
        Added,
        Expired,
        Rejected,
    }

    private sealed class ReferenceAuthorizationStore
    {
        private readonly Dictionary<string, DateTimeOffset> _entries = new(StringComparer.Ordinal);
        private readonly int _capacity;

        public ReferenceAuthorizationStore(int capacity) => _capacity = capacity;

        public int Count => _entries.Count;

        public RegisterResult Register(string nonce, DateTimeOffset expires, DateTimeOffset now)
        {
            if (expires <= now)
                return RegisterResult.Expired;

            PruneExpired(now);
            if (_entries.ContainsKey(nonce) || _entries.Count >= _capacity)
                return RegisterResult.Rejected;

            _entries.Add(nonce, expires);
            return RegisterResult.Added;
        }

        public bool TryConsume(string nonce, DateTimeOffset now)
        {
            if (!_entries.Remove(nonce, out var expires))
                return false;

            return expires > now;
        }

        public int PruneExpired(DateTimeOffset now)
        {
            var expired = _entries
                .Where(pair => pair.Value <= now)
                .Select(pair => pair.Key)
                .ToArray();

            foreach (var nonce in expired)
                _entries.Remove(nonce);

            return expired.Length;
        }

        public void Clear() => _entries.Clear();
    }
}
