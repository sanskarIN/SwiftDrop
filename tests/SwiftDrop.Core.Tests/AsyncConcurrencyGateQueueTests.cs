using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class AsyncConcurrencyGateQueueTests
{
    [Fact]
    public async Task RestrictiveHeadWaiter_PreservesFifoUntilItCanEnter()
    {
        var gate = new AsyncConcurrencyGate();
        var first = await gate.EnterAsync(3);
        var second = await gate.EnterAsync(3);
        var restrictiveHead = gate.EnterAsync(1).AsTask();
        var eligibleFollower = gate.EnterAsync(3).AsTask();

        Assert.False(restrictiveHead.IsCompleted);
        Assert.False(eligibleFollower.IsCompleted);

        await second.DisposeAsync();
        Assert.False(restrictiveHead.IsCompleted);
        Assert.False(eligibleFollower.IsCompleted);

        await first.DisposeAsync();
        await using var restrictiveLease = await restrictiveHead.WaitAsync(TimeSpan.FromSeconds(1));
        await using var followerLease = await eligibleFollower.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CancelledRestrictiveHeadWaiter_AllowsEligibleFollowerToProceed()
    {
        var gate = new AsyncConcurrencyGate();
        await using var first = await gate.EnterAsync(3);
        await using var second = await gate.EnterAsync(3);
        using var cts = new CancellationTokenSource();

        var restrictiveHead = gate.EnterAsync(1, cts.Token).AsTask();
        var eligibleFollower = gate.EnterAsync(3).AsTask();

        Assert.False(restrictiveHead.IsCompleted);
        Assert.False(eligibleFollower.IsCompleted);

        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => restrictiveHead);

        await using var followerLease = await eligibleFollower.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.True(eligibleFollower.IsCompletedSuccessfully);
    }
}
