using System.Net;
using SwiftDrop.Core.Discovery;
using SwiftDrop.Core.Models;

namespace SwiftDrop.Core.Tests;

public sealed class MdnsCodecTests
{
    [Fact]
    public void CreateQuery_IsRecognized_As_SwiftDrop_Query()
    {
        Assert.True(MdnsCodec.IsDiscoveryQuery(MdnsCodec.CreateQuery()));
    }

    [Fact]
    public void Announcement_RoundTrips_Peer_Metadata()
    {
        var now = DateTimeOffset.UtcNow;
        var peer = new PeerDevice(
            "device123",
            "Laptop",
            "Windows",
            "",
            47821,
            new string('A', 64));
        var packet = MdnsCodec.CreateAnnouncement(peer, IPAddress.Parse("192.168.1.22"));

        var parsed = MdnsCodec.TryParseAnnouncement(packet, IPAddress.Loopback, now);

        Assert.NotNull(parsed);
        Assert.Equal(peer.Id, parsed.Id);
        Assert.Equal(peer.Name, parsed.Name);
        Assert.Equal(peer.Platform, parsed.Platform);
        Assert.Equal(peer.Port, parsed.Port);
        Assert.Equal(peer.CertificateFingerprint, parsed.CertificateFingerprint);
        Assert.Equal("192.168.1.22", parsed.Host);
    }

    [Fact]
    public void Parser_Rejects_Truncated_And_Random_Packets()
    {
        Assert.Null(MdnsCodec.TryParseAnnouncement([0, 1, 2], IPAddress.Loopback, DateTimeOffset.UtcNow));
        Assert.Null(MdnsCodec.TryParseAnnouncement(new byte[64], IPAddress.Loopback, DateTimeOffset.UtcNow));
    }
}
