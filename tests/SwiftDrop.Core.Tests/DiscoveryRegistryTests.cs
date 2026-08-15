using SwiftDrop.Core.Discovery;
using SwiftDrop.Core.Models;

namespace SwiftDrop.Core.Tests;

public sealed class DiscoveryRegistryTests
{
    [Fact]
    public void Upsert_Deduplicates_By_Device_Id()
    {
        var registry = new DiscoveryRegistry(TimeSpan.FromSeconds(10));
        var now = DateTimeOffset.UtcNow;
        registry.Upsert(new PeerDevice("a", "One", "Windows", "10.0.0.2", 40000), now);
        registry.Upsert(new PeerDevice("a", "Two", "Windows", "10.0.0.3", 40000), now.AddSeconds(1));

        var peer = Assert.Single(registry.Snapshot());
        Assert.Equal("Two", peer.Name);
        Assert.Equal("10.0.0.3", peer.Host);
    }

    [Fact]
    public void RemoveExpired_Removes_Stale_Peers()
    {
        var registry = new DiscoveryRegistry(TimeSpan.FromSeconds(5));
        var now = DateTimeOffset.UtcNow;
        registry.Upsert(new PeerDevice("a", "One", "Android", "10.0.0.2", 40000), now);

        Assert.True(registry.RemoveExpired(now.AddSeconds(6)));
        Assert.Empty(registry.Snapshot());
    }

    [Fact]
    public void RemoveExpired_Removes_Peer_At_Exact_Expiry_Boundary()
    {
        var registry = new DiscoveryRegistry(TimeSpan.FromSeconds(5));
        var now = new DateTimeOffset(2026, 8, 15, 9, 30, 0, TimeSpan.Zero);
        registry.Upsert(new PeerDevice("a", "One", "Android", "10.0.0.2", 40000), now);

        Assert.True(registry.RemoveExpired(now.AddSeconds(5)));
        Assert.Empty(registry.Snapshot());
    }

    [Fact]
    public void Snapshot_Can_Exclude_Self()
    {
        var registry = new DiscoveryRegistry();
        registry.Upsert(new PeerDevice("self", "Self", "Windows", "127.0.0.1", 40000), DateTimeOffset.UtcNow);
        registry.Upsert(new PeerDevice("peer", "Peer", "Android", "10.0.0.2", 40000), DateTimeOffset.UtcNow);

        var peer = Assert.Single(registry.Snapshot("self"));
        Assert.Equal("peer", peer.Id);
    }
}
