using SwiftDrop.Core.Networking;

namespace SwiftDrop.Core.Tests;

public sealed class AsyncSessionTrackerTests
{
    [Fact]
    public async Task Track_RemovesCompletedSession()
    {
        var tracker = new AsyncSessionTracker();
        tracker.Track(Task.CompletedTask);
        await tracker.DrainAsync();
        Assert.Equal(0, tracker.Count);
    }

    [Fact]
    public async Task DrainAsync_WaitsForAllActiveSessions()
    {
        var tracker = new AsyncSessionTracker();
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        tracker.Track(first.Task);
        tracker.Track(second.Task);

        var drain = tracker.DrainAsync();
        Assert.False(drain.IsCompleted);
        first.SetResult();
        await Task.Yield();
        Assert.False(drain.IsCompleted);
        second.SetResult();
        await drain;
        Assert.Equal(0, tracker.Count);
    }

    [Fact]
    public async Task DrainAsync_DrainsFaultedSessionWithoutThrowing()
    {
        var tracker = new AsyncSessionTracker();
        tracker.Track(Task.FromException(new IOException("synthetic")));
        await tracker.DrainAsync();
        Assert.Equal(0, tracker.Count);
    }

    [Fact]
    public async Task DrainAsync_CanBeCancelledWhileSessionIsStillRunning()
    {
        var tracker = new AsyncSessionTracker();
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        tracker.Track(pending.Task);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tracker.DrainAsync(cts.Token));
        Assert.Equal(1, tracker.Count);
        pending.SetResult();
        await tracker.DrainAsync();
    }

    [Fact]
    public async Task DrainAsync_HandlesSessionAddedWhileDraining()
    {
        var tracker = new AsyncSessionTracker();
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        tracker.Track(first.Task);

        var drain = tracker.DrainAsync();
        tracker.Track(second.Task);
        first.SetResult();
        await Task.Yield();
        Assert.False(drain.IsCompleted);
        second.SetResult();
        await drain;
        Assert.Equal(0, tracker.Count);
    }
}
