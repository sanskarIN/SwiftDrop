using SwiftDrop.Core.Models;
using SwiftDrop.Core.Storage;

namespace SwiftDrop.Core.Tests;

public sealed class TrustStoreTests : IAsyncLifetime
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"swiftdrop-trust-{Guid.NewGuid():N}.db");
    private TrustStore _store = null!;

    public async Task InitializeAsync()
    {
        _store = new TrustStore(_path);
        await _store.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        foreach (var suffix in new[] { string.Empty, "-shm", "-wal" })
        {
            var candidate = _path + suffix;
            if (File.Exists(candidate)) File.Delete(candidate);
        }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task UpsertGetAndListTrustedPeer()
    {
        var now = DateTimeOffset.UtcNow;
        var peer = new TrustedPeer("device-1", "Laptop", "AA11", now, now);

        await _store.UpsertAsync(peer);

        var loaded = await _store.GetAsync("device-1");
        Assert.NotNull(loaded);
        Assert.Equal("Laptop", loaded!.DeviceName);
        Assert.Single(await _store.GetAllAsync());
    }

    [Fact]
    public async Task RemoveAndClearDeletePeers()
    {
        var now = DateTimeOffset.UtcNow;
        await _store.UpsertAsync(new TrustedPeer("a", "A", "01", now, now));
        await _store.UpsertAsync(new TrustedPeer("b", "B", "02", now, now));

        await _store.RemoveAsync("a");
        Assert.Null(await _store.GetAsync("a"));
        Assert.Single(await _store.GetAllAsync());

        await _store.ClearAsync();
        Assert.Empty(await _store.GetAllAsync());
    }
}
