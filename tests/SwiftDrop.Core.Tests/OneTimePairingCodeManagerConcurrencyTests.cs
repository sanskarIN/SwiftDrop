using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class OneTimePairingCodeManagerConcurrencyTests
{
    [Fact]
    public async Task TryConsume_AllowsExactlyOneConcurrentWinner()
    {
        var now = new DateTimeOffset(2026, 8, 19, 1, 45, 0, TimeSpan.Zero);
        var manager = new OneTimePairingCodeManager(TimeSpan.FromMinutes(2));
        var snapshot = manager.Create(now);

        var attempts = Enumerable.Range(0, 64)
            .Select(_ => Task.Run(() => manager.TryConsume(snapshot.Code, now.AddSeconds(1))))
            .ToArray();

        var results = await Task.WhenAll(attempts);
        Assert.Single(results, value => value);
        Assert.False(manager.TryConsume(snapshot.Code, now.AddSeconds(2)));
    }
}
