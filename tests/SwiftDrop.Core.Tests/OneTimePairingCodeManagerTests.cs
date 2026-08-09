using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class OneTimePairingCodeManagerTests
{
    [Fact]
    public void Create_ProducesEightDigits_AndConsumesOnce()
    {
        var now = new DateTimeOffset(2026, 8, 9, 6, 45, 0, TimeSpan.Zero);
        var manager = new OneTimePairingCodeManager(TimeSpan.FromMinutes(2));
        var snapshot = manager.Create(now);

        Assert.Equal(8, snapshot.Code.Length);
        Assert.All(snapshot.Code, c => Assert.InRange(c, '0', '9'));
        Assert.True(manager.TryConsume(snapshot.Code, now.AddSeconds(10)));
        Assert.False(manager.TryConsume(snapshot.Code, now.AddSeconds(11)));
    }

    [Fact]
    public void TryConsume_RejectsInvalidAndExpiredCodes()
    {
        var now = DateTimeOffset.UtcNow;
        var manager = new OneTimePairingCodeManager(TimeSpan.FromSeconds(30));
        var snapshot = manager.Create(now);

        Assert.False(manager.TryConsume("123", now));
        Assert.False(manager.TryConsume("abcdefgh", now));
        Assert.False(manager.TryConsume(snapshot.Code, now.AddMinutes(1)));
    }

    [Fact]
    public void Create_ReplacesPreviousCode()
    {
        var now = DateTimeOffset.UtcNow;
        var manager = new OneTimePairingCodeManager();
        var first = manager.Create(now);
        var second = manager.Create(now.AddSeconds(1));

        if (!string.Equals(first.Code, second.Code, StringComparison.Ordinal))
            Assert.False(manager.TryConsume(first.Code, now.AddSeconds(2)));
        Assert.True(manager.TryConsume(second.Code, now.AddSeconds(2)));
    }
}
