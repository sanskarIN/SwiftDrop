using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class AsyncConcurrencyGateTests
{
    [Fact]
    public async Task EnterAsync_QueuesWhenLimitReached_AndReleasesNext()
    {
        var gate = new AsyncConcurrencyGate();
        await using var first = await gate.EnterAsync(1);
        var waiting = gate.EnterAsync(1).AsTask();

        Assert.False(waiting.IsCompleted);
        await first.DisposeAsync();
        await using var second = await waiting.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.True(waiting.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task EnterAsync_AllowsRequestedConcurrency()
    {
        var gate = new AsyncConcurrencyGate();
        await using var first = await gate.EnterAsync(2);
        await using var second = await gate.EnterAsync(2);
        var third = gate.EnterAsync(2).AsTask();

        Assert.False(third.IsCompleted);
        await first.DisposeAsync();
        await using var thirdLease = await third.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65)]
    public void EnterAsync_RejectsUnsupportedLimits(int limit)
    {
        var gate = new AsyncConcurrencyGate();

        Assert.Throws<ArgumentOutOfRangeException>(() => gate.EnterAsync(limit));
    }

    [Fact]
    public async Task EnterAsync_PreCancelledTokenDoesNotConsumeSlot()
    {
        var gate = new AsyncConcurrencyGate();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() => gate.EnterAsync(1, cts.Token));
        await using var lease = await gate.EnterAsync(1).AsTask().WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task EnterAsync_QueuedCancellationDoesNotConsumeSlot()
    {
        var gate = new AsyncConcurrencyGate();
        await using var first = await gate.EnterAsync(1);
        using var cts = new CancellationTokenSource();
        var cancelled = gate.EnterAsync(1, cts.Token).AsTask();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
        await first.DisposeAsync();
        await using var next = await gate.EnterAsync(1).AsTask().WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Lease_DisposeAsync_IsIdempotent()
    {
        var gate = new AsyncConcurrencyGate();
        var lease = await gate.EnterAsync(1);

        await lease.DisposeAsync();
        await lease.DisposeAsync();

        await using var next = await gate.EnterAsync(1).AsTask().WaitAsync(TimeSpan.FromSeconds(1));
    }
}
