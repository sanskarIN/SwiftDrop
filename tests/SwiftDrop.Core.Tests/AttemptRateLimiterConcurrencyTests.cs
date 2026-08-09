using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class AttemptRateLimiterConcurrencyTests
{
    [Fact]
    public async Task TryAcquire_NeverGrantsMoreThanConfiguredLimitConcurrently()
    {
        var limiter = new AttemptRateLimiter(8, TimeSpan.FromMinutes(1));
        var now = DateTimeOffset.UtcNow;
        var tasks = Enumerable.Range(0, 128)
            .Select(_ => Task.Run(() => limiter.TryAcquire("same-peer", now)))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        Assert.Equal(8, results.Count(x => x));
    }

    [Fact]
    public void TryAcquire_AllowsFreshWindowAfterExpiry()
    {
        var limiter = new AttemptRateLimiter(2, TimeSpan.FromSeconds(30));
        var now = DateTimeOffset.UtcNow;

        Assert.True(limiter.TryAcquire("peer", now));
        Assert.True(limiter.TryAcquire("peer", now.AddSeconds(1)));
        Assert.False(limiter.TryAcquire("peer", now.AddSeconds(2)));
        Assert.True(limiter.TryAcquire("peer", now.AddSeconds(31)));
    }

    [Fact]
    public void TryAcquire_IsolatesIndependentKeys()
    {
        var limiter = new AttemptRateLimiter(1, TimeSpan.FromMinutes(1));
        var now = DateTimeOffset.UtcNow;

        Assert.True(limiter.TryAcquire("peer-a", now));
        Assert.False(limiter.TryAcquire("peer-a", now));
        Assert.True(limiter.TryAcquire("peer-b", now));
    }
}
