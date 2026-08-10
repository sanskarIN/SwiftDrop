using System.Text;
using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class Utf8TextLimiterTests
{
    [Fact]
    public void Truncate_ReturnsOriginalWhenAlreadyWithinLimit()
        => Assert.Equal("hello", Utf8TextLimiter.Truncate("hello", 5));

    [Fact]
    public void Truncate_StopsAtAsciiByteBoundary()
        => Assert.Equal("abc", Utf8TextLimiter.Truncate("abcdef", 3));

    [Fact]
    public void Truncate_DoesNotSplitThreeByteRune()
    {
        var value = "A€B";
        var result = Utf8TextLimiter.Truncate(value, 3);
        Assert.Equal("A", result);
        Assert.True(Encoding.UTF8.GetByteCount(result) <= 3);
    }

    [Fact]
    public void Truncate_DoesNotSplitSurrogatePair()
    {
        var value = "A😀B";
        Assert.Equal("A", Utf8TextLimiter.Truncate(value, 4));
        Assert.Equal("A😀", Utf8TextLimiter.Truncate(value, 5));
    }

    [Fact]
    public void Truncate_HandlesZeroByteLimit()
        => Assert.Equal(string.Empty, Utf8TextLimiter.Truncate("abc", 0));

    [Fact]
    public void Truncate_RejectsNegativeLimit()
        => Assert.Throws<ArgumentOutOfRangeException>(() => Utf8TextLimiter.Truncate("abc", -1));
}
