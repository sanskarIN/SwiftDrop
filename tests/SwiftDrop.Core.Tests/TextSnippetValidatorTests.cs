using SwiftDrop.Core.Protocol;

namespace SwiftDrop.Core.Tests;

public sealed class TextSnippetValidatorTests
{
    [Fact]
    public void Validate_Accepts_Bounded_Fresh_Text()
    {
        var now = DateTimeOffset.UtcNow;
        TextSnippetValidator.Validate("hello", now.AddMinutes(1), now);
    }

    [Fact]
    public void Validate_Rejects_Empty_Text()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<InvalidDataException>(() => TextSnippetValidator.Validate("   ", now.AddMinutes(1), now));
    }

    [Fact]
    public void Validate_Rejects_Expired_Text()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<InvalidDataException>(() => TextSnippetValidator.Validate("hello", now.AddSeconds(-1), now));
    }

    [Fact]
    public void Validate_Rejects_Excessively_Distant_Expiry()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<InvalidDataException>(() => TextSnippetValidator.Validate("hello", now.AddMinutes(10), now));
    }

    [Fact]
    public void Validate_Rejects_Text_Over_Utf8_Byte_Limit()
    {
        var now = DateTimeOffset.UtcNow;
        var text = new string('é', ProtocolConstants.MaxTextBytes);
        Assert.Throws<InvalidDataException>(() => TextSnippetValidator.Validate(text, now.AddMinutes(1), now));
    }
}
