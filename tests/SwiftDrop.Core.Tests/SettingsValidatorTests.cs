using SwiftDrop.Core.Configuration;
using SwiftDrop.Core.Models;

namespace SwiftDrop.Core.Tests;

public sealed class SettingsValidatorTests
{
    [Fact]
    public void AcceptsDefaults()
        => Assert.Equal(AppSettings.Default, SettingsValidator.Validate(AppSettings.Default));

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    public void RejectsInvalidConcurrency(int concurrency)
    {
        var settings = AppSettings.Default with { TransferConcurrency = concurrency };
        Assert.Throws<ArgumentOutOfRangeException>(() => SettingsValidator.Validate(settings));
    }

    [Fact]
    public void NormalizesThemeCasing()
    {
        var result = SettingsValidator.Validate(AppSettings.Default with { Theme = "dark" });
        Assert.Equal("Dark", result.Theme);
    }
}
