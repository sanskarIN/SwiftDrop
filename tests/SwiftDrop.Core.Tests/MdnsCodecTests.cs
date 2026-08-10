using System.Net;
using System.Text;
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

    [Fact]
    public void QueryParser_RejectsCompressionPointerLoop()
    {
        var packet = new byte[18];
        packet[4] = 0;
        packet[5] = 1;
        packet[12] = 0xC0;
        packet[13] = 0x0C;
        packet[14] = 0;
        packet[15] = 12;
        packet[16] = 0;
        packet[17] = 1;

        Assert.False(MdnsCodec.IsDiscoveryQuery(packet));
    }

    [Fact]
    public void AnnouncementParser_RejectsCompressionPointerLoop()
    {
        var packet = new byte[24];
        packet[2] = 0x80;
        packet[7] = 1;
        packet[12] = 0xC0;
        packet[13] = 0x0C;

        Assert.Null(MdnsCodec.TryParseAnnouncement(packet, IPAddress.Loopback, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void AnnouncementParser_RejectsDuplicateTxtKeys()
    {
        var peer = new PeerDevice(
            "device123",
            "Laptop",
            "Windows",
            "",
            47821,
            new string('A', 64));
        var packet = MdnsCodec.CreateAnnouncement(peer, IPAddress.Parse("192.168.1.22"));
        var original = Encoding.UTF8.GetBytes("name=Laptop");
        var replacement = Encoding.UTF8.GetBytes("id=other123");
        Assert.Equal(original.Length, replacement.Length);
        var index = IndexOf(packet, original);
        Assert.True(index >= 0);
        replacement.CopyTo(packet.AsSpan(index, replacement.Length));

        Assert.Null(MdnsCodec.TryParseAnnouncement(packet, IPAddress.Loopback, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void QueryParser_RejectsImpossibleQuestionCountOnShortPacket()
    {
        var packet = new byte[12];
        packet[4] = 0xFF;
        packet[5] = 0xFF;
        Assert.False(MdnsCodec.IsDiscoveryQuery(packet));
    }

    [Fact]
    public void AnnouncementParser_RejectsEveryTruncatedPrefixOfValidAnnouncement()
    {
        var peer = new PeerDevice(
            "device123",
            "Laptop",
            "Windows",
            "",
            47821,
            new string('A', 64));
        var packet = MdnsCodec.CreateAnnouncement(peer, IPAddress.Parse("192.168.1.22"));

        for (var length = 0; length < packet.Length; length++)
        {
            var truncated = packet.AsSpan(0, length).ToArray();
            Assert.Null(MdnsCodec.TryParseAnnouncement(truncated, IPAddress.Loopback, DateTimeOffset.UtcNow));
        }
    }

    private static int IndexOf(byte[] haystack, byte[] needle)
    {
        for (var i = 0; i <= haystack.Length - needle.Length; i++)
        {
            if (haystack.AsSpan(i, needle.Length).SequenceEqual(needle)) return i;
        }
        return -1;
    }
}
