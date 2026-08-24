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
    public void Validate_Rejects_Expiry_At_Current_Time()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<InvalidDataException>(() => TextSnippetValidator.Validate("hello", now, now));
    }

    [Fact]
    public void Validate_Accepts_Latest_Clock_Skew_Boundary()
    {
        var now = DateTimeOffset.UtcNow;
        var latest = now.Add(ProtocolConstants.TextSnippetLifetime).AddSeconds(30);

        TextSnippetValidator.Validate("hello", latest, now);
    }

    [Fact]
    public void Validate_Rejects_Expiry_Beyond_Clock_Skew_Boundary()
    {
        var now = DateTimeOffset.UtcNow;
        var tooLate = now.Add(ProtocolConstants.TextSnippetLifetime).AddSeconds(31);

        Assert.Throws<InvalidDataException>(() => TextSnippetValidator.Validate("hello", tooLate, now));
    }

    [Fact]
    public void Validate_Rejects_Excessively_Distant_Expiry()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<InvalidDataException>(() => TextSnippetValidator.Validate("hello", now.AddMinutes(10), now));
    }

    [Fact]
    public void Validate_Accepts_Text_At_Utf8_Byte_Limit()
    {
        var now = DateTimeOffset.UtcNow;
        var text = new string('a', ProtocolConstants.MaxTextSnippetBytes);

        TextSnippetValidator.Validate(text, now.AddMinutes(1), now);
    }

    [Fact]
    public void Validate_Rejects_Text_Over_Utf8_Byte_Limit()
    {
        var now = DateTimeOffset.UtcNow;
        var text = new string('é', ProtocolConstants.MaxTextSnippetBytes);
        Assert.Throws<InvalidDataException>(() => TextSnippetValidator.Validate(text, now.AddMinutes(1), now));
    }
}
