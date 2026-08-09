using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class PairingCodecTests
{
    [Fact]
    public void RoundTrip_PreservesPayload()
    {
        var now = DateTimeOffset.UtcNow;
        var payload = new PairingPayload("1", "abc", "Laptop", "192.168.1.20", 47821, new string('A', 64), PairingCodec.CreateNonce(), now.AddMinutes(2).ToUnixTimeSeconds());
        var decoded = PairingCodec.Decode(PairingCodec.Encode(payload), now);
        Assert.Equal(payload, decoded);
    }

    [Fact]
    public void Decode_RejectsExpiredPayload()
    {
        var now = DateTimeOffset.UtcNow;
        var payload = new PairingPayload("1", "abc", "Laptop", "192.168.1.20", 47821, new string('A', 64), PairingCodec.CreateNonce(), now.AddSeconds(-1).ToUnixTimeSeconds());
        Assert.Throws<InvalidOperationException>(() => PairingCodec.Decode(PairingCodec.Encode(payload), now));
    }

    [Fact]
    public void Decode_RejectsWrongScheme() => Assert.Throws<FormatException>(() => PairingCodec.Decode("https://example.com"));
}
