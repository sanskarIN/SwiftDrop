using SwiftDrop.Core.Networking;

namespace SwiftDrop.Core.Tests;

public sealed class AsyncSessionTrackerStressTests
{
    [Fact]
    public async Task DrainAsync_CompletesAfterMixedSeededSessionOutcomes()
    {
        const int sessionCount = 128;
        var tracker = new AsyncSessionTracker();
        var random = new Random(0x5E5510);
        var sessions = Enumerable.Range(0, sessionCount)
            .Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
            .ToArray();

        foreach (var session in sessions)
            tracker.Track(session.Task);

        Assert.Equal(sessionCount, tracker.Count);
        var drain = tracker.DrainAsync();
        Assert.False(drain.IsCompleted);

        foreach (var index in Enumerable.Range(0, sessionCount).OrderBy(_ => random.Next()))
        {
            if (index % 5 == 0)
                sessions[index].SetException(new IOException("synthetic session failure"));
            else
                sessions[index].SetResult();
        }

        await drain.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(0, tracker.Count);
    }
}
