using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class AttemptRateLimiterTests
{
    [Fact]
    public void RejectsAttemptsBeyondWindowLimit()
    {
        var limiter = new AttemptRateLimiter(2, TimeSpan.FromMinutes(1));
        var now = DateTimeOffset.UtcNow;

        Assert.True(limiter.TryAcquire("peer", now));
        Assert.True(limiter.TryAcquire("peer", now.AddSeconds(1)));
        Assert.False(limiter.TryAcquire("peer", now.AddSeconds(2)));
        Assert.True(limiter.TryAcquire("peer", now.AddMinutes(1).AddSeconds(1)));
    }

    [Fact]
    public void TracksKeysIndependently()
    {
        var limiter = new AttemptRateLimiter(1, TimeSpan.FromMinutes(5));
        var now = DateTimeOffset.UtcNow;

        Assert.True(limiter.TryAcquire("a", now));
        Assert.True(limiter.TryAcquire("b", now));
        Assert.False(limiter.TryAcquire("a", now));
    }
}
