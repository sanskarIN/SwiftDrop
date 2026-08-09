using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class ProtocolCompatibilityTests
{
    [Fact]
    public void CurrentProtocolVersion_IsExplicit()
        => Assert.Equal("1", ProtocolConstants.CurrentVersion);

    [Fact]
    public void PairingCodec_RejectsUnknownFutureVersion()
    {
        var now = DateTimeOffset.UtcNow;
        var payload = new PairingPayload(
            "999",
            "device-id",
            "Device",
            "192.168.1.20",
            ProtocolConstants.DefaultPort,
            new string('A', 64),
            PairingCodec.CreateNonce(),
            now.AddMinutes(1).ToUnixTimeSeconds());

        var link = PairingCodec.Encode(payload);
        Assert.Throws<NotSupportedException>(() => PairingCodec.Decode(link, now));
    }

    [Fact]
    public void PairingCodec_RejectsExcessiveInvitationLifetime()
    {
        var now = DateTimeOffset.UtcNow;
        var payload = new PairingPayload(
            ProtocolConstants.CurrentVersion,
            "device-id",
            "Device",
            "192.168.1.20",
            ProtocolConstants.DefaultPort,
            new string('A', 64),
            PairingCodec.CreateNonce(),
            now.AddMinutes(30).ToUnixTimeSeconds());

        var link = PairingCodec.Encode(payload);
        Assert.Throws<InvalidOperationException>(() => PairingCodec.Decode(link, now));
    }
}
