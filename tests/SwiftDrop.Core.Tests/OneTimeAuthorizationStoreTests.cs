using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class OneTimeAuthorizationStoreTests
{
    [Fact]
    public void TryConsume_AllowsExactlyOneUse()
    {
        var store = new OneTimeAuthorizationStore();
        var now = DateTimeOffset.UtcNow;
        var nonce = Nonce('A');
        store.Register(nonce, now.AddMinutes(2), now);

        Assert.True(store.TryConsume(nonce, now.AddSeconds(1)));
        Assert.False(store.TryConsume(nonce, now.AddSeconds(2)));
    }

    [Fact]
    public void TryConsume_RejectsExpiredAuthorization()
    {
        var store = new OneTimeAuthorizationStore();
        var now = DateTimeOffset.UtcNow;
        var nonce = Nonce('B');
        store.Register(nonce, now.AddSeconds(10), now);

        Assert.False(store.TryConsume(nonce, now.AddSeconds(11)));
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public async Task TryConsume_ConcurrentCallersHaveSingleWinner()
    {
        var store = new OneTimeAuthorizationStore();
        var now = DateTimeOffset.UtcNow;
        var nonce = Nonce('C');
        store.Register(nonce, now.AddMinutes(1), now);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 64)
                .Select(_ => Task.Run(() => store.TryConsume(nonce, now.AddSeconds(1)))));

        Assert.Equal(1, results.Count(x => x));
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public void Register_RejectsDuplicateActiveNonce()
    {
        var store = new OneTimeAuthorizationStore();
        var now = DateTimeOffset.UtcNow;
        var nonce = Nonce('D');
        store.Register(nonce, now.AddMinutes(1), now);
        Assert.Throws<InvalidOperationException>(() => store.Register(nonce, now.AddMinutes(1), now));
    }

    [Fact]
    public void Register_EnforcesBoundedActiveSet()
    {
        var store = new OneTimeAuthorizationStore(2);
        var now = DateTimeOffset.UtcNow;
        store.Register(Nonce('E'), now.AddMinutes(1), now);
        store.Register(Nonce('F'), now.AddMinutes(1), now);
        Assert.Throws<InvalidOperationException>(() => store.Register(Nonce('G'), now.AddMinutes(1), now));
    }

    [Fact]
    public void PruneExpired_ReclaimsCapacity()
    {
        var store = new OneTimeAuthorizationStore(1);
        var now = DateTimeOffset.UtcNow;
        store.Register(Nonce('H'), now.AddSeconds(5), now);

        Assert.Equal(1, store.PruneExpired(now.AddSeconds(6)));
        store.Register(Nonce('I'), now.AddMinutes(1), now.AddSeconds(6));
        Assert.Equal(1, store.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("contains+plus-character")]
    public void Register_RejectsMalformedNonce(string nonce)
    {
        var store = new OneTimeAuthorizationStore();
        var now = DateTimeOffset.UtcNow;
        Assert.ThrowsAny<ArgumentException>(() => store.Register(nonce, now.AddMinutes(1), now));
    }

    private static string Nonce(char value) => new(value, 24);
}
