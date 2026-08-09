namespace SwiftDrop.Core.Transfer;

public sealed class AsyncConcurrencyGate
{
    private readonly object _gate = new();
    private readonly Queue<Waiter> _waiters = new();
    private int _active;

    public ValueTask<IAsyncDisposable> EnterAsync(int limit, CancellationToken ct = default)
    {
        if (limit is < 1 or > 64) throw new ArgumentOutOfRangeException(nameof(limit));
        ct.ThrowIfCancellationRequested();

        Waiter waiter;
        lock (_gate)
        {
            if (_active < limit && _waiters.Count == 0)
            {
                _active++;
                return ValueTask.FromResult<IAsyncDisposable>(new Lease(this));
            }

            waiter = new Waiter(limit);
            _waiters.Enqueue(waiter);
        }

        if (ct.CanBeCanceled)
        {
            var registration = ct.Register(static state =>
            {
                var tuple = (Tuple<AsyncConcurrencyGate, Waiter, CancellationToken>)state!;
                tuple.Item1.Cancel(tuple.Item2, tuple.Item3);
            }, Tuple.Create(this, waiter, ct));

            lock (_gate)
            {
                if (waiter.Completed)
                    registration.Dispose();
                else
                    waiter.Registration = registration;
            }
        }

        return new ValueTask<IAsyncDisposable>(waiter.Completion.Task);
    }

    private void Cancel(Waiter waiter, CancellationToken ct)
    {
        lock (_gate)
        {
            if (waiter.Completed) return;
            waiter.Completed = true;
            waiter.Registration.Dispose();
            waiter.Completion.TrySetCanceled(ct);
            DispatchLocked();
        }
    }

    private void Release()
    {
        lock (_gate)
        {
            if (_active <= 0) throw new InvalidOperationException("Concurrency gate release imbalance.");
            _active--;
            DispatchLocked();
        }
    }

    private void DispatchLocked()
    {
        while (_waiters.Count > 0)
        {
            var waiter = _waiters.Peek();
            if (waiter.Completed)
            {
                _waiters.Dequeue();
                continue;
            }
            if (_active >= waiter.Limit) return;

            _waiters.Dequeue();
            waiter.Completed = true;
            waiter.Registration.Dispose();
            _active++;
            waiter.Completion.TrySetResult(new Lease(this));
        }
    }

    private sealed class Waiter
    {
        public Waiter(int limit)
        {
            Limit = limit;
            Completion = new TaskCompletionSource<IAsyncDisposable>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public int Limit { get; }
        public TaskCompletionSource<IAsyncDisposable> Completion { get; }
        public CancellationTokenRegistration Registration { get; set; }
        public bool Completed { get; set; }
    }

    private sealed class Lease : IAsyncDisposable
    {
        private AsyncConcurrencyGate? _owner;

        public Lease(AsyncConcurrencyGate owner) => _owner = owner;

        public ValueTask DisposeAsync()
        {
            Interlocked.Exchange(ref _owner, null)?.Release();
            return ValueTask.CompletedTask;
        }
    }
}
