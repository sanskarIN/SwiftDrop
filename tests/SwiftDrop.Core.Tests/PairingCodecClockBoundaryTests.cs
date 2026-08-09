using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class PairingCodecClockBoundaryTests
{
    [Fact]
    public void Validate_RejectsInvitationExpiringAtCurrentInstant()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var payload = Valid(now) with { ExpiresUnixSeconds = now.ToUnixTimeSeconds() };
        Assert.Throws<InvalidOperationException>(() => PairingCodec.Validate(payload, now));
    }

    [Fact]
    public void Validate_AcceptsInvitationExpiringOneSecondInFuture()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var payload = Valid(now) with { ExpiresUnixSeconds = now.AddSeconds(1).ToUnixTimeSeconds() };
        var validated = PairingCodec.Validate(payload, now);
        Assert.Equal(payload.ExpiresUnixSeconds, validated.ExpiresUnixSeconds);
    }

    [Fact]
    public void Validate_RejectsLifetimeWellBeyondProtocolAllowance()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var payload = Valid(now) with
        {
            ExpiresUnixSeconds = now.Add(ProtocolConstants.PairingLifetime).AddMinutes(5).ToUnixTimeSeconds()
        };
        Assert.Throws<InvalidOperationException>(() => PairingCodec.Validate(payload, now));
    }

    [Fact]
    public void Validate_NormalizesFingerprintAndHostWithoutChangingAuthorization()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var payload = Valid(now) with
        {
            CertificateFingerprint = string.Join(':', Enumerable.Repeat("aa", 32)),
            Host = "[fd00::20]"
        };

        var validated = PairingCodec.Validate(payload, now);

        Assert.Equal(new string('A', 64), validated.CertificateFingerprint);
        Assert.Equal("fd00::20", validated.Host);
        Assert.Equal(payload.Nonce, validated.Nonce);
    }

    private static PairingPayload Valid(DateTimeOffset now)
        => new(
            ProtocolConstants.CurrentVersion,
            "clock-test-device",
            "Clock Test",
            "192.168.1.20",
            ProtocolConstants.DefaultPort,
            new string('A', 64),
            PairingCodec.CreateNonce(),
            now.Add(ProtocolConstants.PairingLifetime).ToUnixTimeSeconds());
}
