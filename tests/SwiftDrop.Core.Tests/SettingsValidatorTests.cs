using SwiftDrop.Core.Configuration;
using SwiftDrop.Core.Models;

namespace SwiftDrop.Core.Tests;

public sealed class SettingsValidatorTests
{
    [Fact]
    public void AcceptsDefaults()
        => Assert.Equal(AppSettings.Default, SettingsValidator.Validate(AppSettings.Default));

    [Fact]
    public void OptionalNotifications_AreDisabledByDefault()
        => Assert.False(AppSettings.Default.NotificationsEnabled);

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    public void RejectsInvalidConcurrency(int concurrency)
    {
        var settings = AppSettings.Default with { TransferConcurrency = concurrency };
        Assert.Throws<ArgumentOutOfRangeException>(() => SettingsValidator.Validate(settings));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    public void AcceptsConcurrencyBoundary(int concurrency)
        => Assert.Equal(
            concurrency,
            SettingsValidator.Validate(AppSettings.Default with { TransferConcurrency = concurrency }).TransferConcurrency);

    [Theory]
    [InlineData(-1)]
    [InlineData(3651)]
    public void RejectsInvalidHistoryRetention(int days)
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            SettingsValidator.Validate(AppSettings.Default with { HistoryRetentionDays = days }));

    [Theory]
    [InlineData(0)]
    [InlineData(3650)]
    public void AcceptsHistoryRetentionBoundary(int days)
        => Assert.Equal(
            days,
            SettingsValidator.Validate(AppSettings.Default with { HistoryRetentionDays = days }).HistoryRetentionDays);

    [Theory]
    [InlineData("system", "System")]
    [InlineData("LIGHT", "Light")]
    [InlineData("dark", "Dark")]
    public void NormalizesThemeCasing(string input, string expected)
        => Assert.Equal(expected, SettingsValidator.Validate(AppSettings.Default with { Theme = input }).Theme);

    [Theory]
    [InlineData("")]
    [InlineData("blue")]
    public void RejectsUnsupportedTheme(string theme)
        => Assert.Throws<ArgumentException>(() =>
            SettingsValidator.Validate(AppSettings.Default with { Theme = theme }));

    [Theory]
    [InlineData("EN", "en")]
    [InlineData("Hi", "hi")]
    public void NormalizesLanguageCasing(string input, string expected)
        => Assert.Equal(expected, SettingsValidator.Validate(AppSettings.Default with { Language = input }).Language);

    [Theory]
    [InlineData("")]
    [InlineData("fr")]
    public void RejectsUnsupportedLanguage(string language)
        => Assert.Throws<ArgumentException>(() =>
            SettingsValidator.Validate(AppSettings.Default with { Language = language }));

    [Fact]
    public void TrimsReceiveFolder()
        => Assert.Equal(
            "incoming",
            SettingsValidator.Validate(AppSettings.Default with { DefaultReceiveFolder = "  incoming  " }).DefaultReceiveFolder);

    [Fact]
    public void RejectsReceiveFolderOverMaximumLength()
        => Assert.Throws<ArgumentException>(() =>
            SettingsValidator.Validate(AppSettings.Default with { DefaultReceiveFolder = new string('a', 1025) }));

    [Fact]
    public void RejectsReceiveFolderContainingControlCharacter()
        => Assert.Throws<ArgumentException>(() =>
            SettingsValidator.Validate(AppSettings.Default with { DefaultReceiveFolder = "incoming\nfolder" }));

    [Fact]
    public void RejectsNullReceiveFolderWithoutNullReferenceFailure()
    {
        var settings = AppSettings.Default with { DefaultReceiveFolder = null! };

        Assert.Throws<ArgumentNullException>(() => SettingsValidator.Validate(settings));
    }
}
