using SwiftDrop.Core.Models;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class ManifestValidatorTests
{
    [Fact]
    public void ValidateEntry_AcceptsWellFormedMetadata()
    {
        var entry = new FileManifestEntry(
            "folder/file.txt",
            12,
            new string('a', 64),
            DateTimeOffset.UtcNow.AddMinutes(-1));

        Assert.Equal(entry, ManifestValidator.ValidateEntry(entry));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    public void IsSha256_RejectsMalformedHashes(string value)
        => Assert.False(ManifestValidator.IsSha256(value));

    [Fact]
    public void ValidateEntry_RejectsFutureTimestamp()
    {
        var entry = new FileManifestEntry(
            "file.txt",
            1,
            new string('0', 64),
            DateTimeOffset.UtcNow.AddDays(5));

        Assert.Throws<InvalidDataException>(() => ManifestValidator.ValidateEntry(entry));
    }
}
