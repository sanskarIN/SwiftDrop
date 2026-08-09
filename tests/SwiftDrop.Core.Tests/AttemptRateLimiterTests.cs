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

    [Fact]
    public void BoundsUniqueKeys_AndAdmitsNewKeyAfterExpiry()
    {
        var limiter = new AttemptRateLimiter(1, TimeSpan.FromSeconds(10), maxKeys: 16);
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < 16; i++)
            Assert.True(limiter.TryAcquire($"peer-{i}", now));

        Assert.False(limiter.TryAcquire("peer-17", now.AddSeconds(1)));
        Assert.True(limiter.TryAcquire("peer-17", now.AddSeconds(11)));
    }
}
