using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class FingerprintTests
{
    [Fact]
    public void FixedTimeEquals_AcceptsEquivalentHex()
    {
        var value = new string('A', 64);
        Assert.True(Fingerprint.FixedTimeEquals(value, value.ToLowerInvariant()));
    }

    [Fact]
    public void FixedTimeEquals_RejectsMalformedHex() => Assert.False(Fingerprint.FixedTimeEquals("zz", "aa"));
}
