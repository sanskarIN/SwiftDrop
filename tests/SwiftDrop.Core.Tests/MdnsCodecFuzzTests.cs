using System.Net;
using SwiftDrop.Core.Discovery;

namespace SwiftDrop.Core.Tests;

public sealed class MdnsCodecFuzzTests
{
    [Fact]
    public void RandomPackets_NeverEscapeParserSafetyBoundary()
    {
        var random = new Random(0x5D45);
        for (var i = 0; i < 2_000; i++)
        {
            var packet = new byte[random.Next(0, 1024)];
            random.NextBytes(packet);

            var queryException = Record.Exception(() => MdnsCodec.IsDiscoveryQuery(packet));
            var announcementException = Record.Exception(() =>
                MdnsCodec.TryParseAnnouncement(packet, IPAddress.Loopback, DateTimeOffset.UnixEpoch));

            Assert.Null(queryException);
            Assert.Null(announcementException);
        }
    }
}
