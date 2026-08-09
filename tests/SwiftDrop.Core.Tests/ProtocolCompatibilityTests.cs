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
        var payload = new PairingPayload(
            "999",
            "device-id",
            "Device",
            "192.168.1.20",
            ProtocolConstants.DefaultPort,
            new string('A', 64),
            PairingCodec.CreateNonce(),
            DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds());

        Assert.Throws<InvalidDataException>(() => PairingCodec.Encode(payload));
    }

    [Fact]
    public void PairingCodec_RejectsExcessiveInvitationLifetime()
    {
        var payload = new PairingPayload(
            ProtocolConstants.CurrentVersion,
            "device-id",
            "Device",
            "192.168.1.20",
            ProtocolConstants.DefaultPort,
            new string('A', 64),
            PairingCodec.CreateNonce(),
            DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds());

        Assert.Throws<InvalidDataException>(() => PairingCodec.Encode(payload));
    }
}
