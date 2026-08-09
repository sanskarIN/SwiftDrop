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
    public void FixedTimeEquals_AcceptsColonSeparatedEquivalentHex()
    {
        var compact = string.Concat(Enumerable.Repeat("AB", 32));
        var pretty = string.Join(':', Enumerable.Repeat("AB", 32));
        Assert.True(Fingerprint.FixedTimeEquals(compact, pretty));
    }

    [Fact]
    public void FixedTimeEquals_RejectsMalformedHex()
        => Assert.False(Fingerprint.FixedTimeEquals("zz", "aa"));

    [Fact]
    public void TryNormalizeSha256_ReturnsCanonicalUppercase()
    {
        var lower = new string('a', 64);
        Assert.True(Fingerprint.TryNormalizeSha256(lower, out var normalized));
        Assert.Equal(new string('A', 64), normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("AA")]
    [InlineData("GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG")]
    public void TryNormalizeSha256_RejectsInvalidValues(string value)
        => Assert.False(Fingerprint.TryNormalizeSha256(value, out _));

    [Fact]
    public void Pretty_ProducesThirtyTwoColonSeparatedBytes()
    {
        var pretty = Fingerprint.Pretty(new string('A', 64));
        Assert.Equal(32, pretty.Split(':').Length);
        Assert.Equal(95, pretty.Length);
    }
}
