using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;
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
        var entry = Entry() with { LastWriteUtc = DateTimeOffset.UtcNow.AddDays(5) };
        Assert.Throws<InvalidDataException>(() => ManifestValidator.ValidateEntry(entry));
    }

    [Fact]
    public void ValidateEntry_RejectsPreUnixTimestamp()
    {
        var entry = Entry() with { LastWriteUtc = DateTimeOffset.UnixEpoch.AddTicks(-1) };
        Assert.Throws<InvalidDataException>(() => ManifestValidator.ValidateEntry(entry));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(long.MaxValue)]
    public void ValidateEntry_RejectsUnsafeLength(long length)
    {
        var entry = Entry() with { Length = length };
        Assert.Throws<InvalidDataException>(() => ManifestValidator.ValidateEntry(entry));
    }

    [Fact]
    public void ValidateEntry_AcceptsConfiguredMaximumFileLength()
    {
        var entry = Entry() with { Length = ProtocolConstants.MaxSingleFileBytes };
        Assert.Equal(entry, ManifestValidator.ValidateEntry(entry));
    }

    [Fact]
    public void ValidateEntry_RejectsControlCharactersInPath()
    {
        var entry = Entry() with { RelativePath = "folder/file\u0001.txt" };
        Assert.Throws<InvalidDataException>(() => ManifestValidator.ValidateEntry(entry));
    }

    [Fact]
    public void ValidateEntry_RejectsOversizedPathMetadata()
    {
        var entry = Entry() with { RelativePath = new string('a', ManifestValidator.MaximumRelativePathLength + 1) };
        Assert.Throws<InvalidDataException>(() => ManifestValidator.ValidateEntry(entry));
    }

    [Theory]
    [InlineData("../escape.txt")]
    [InlineData("folder/../escape.txt")]
    [InlineData("folder/./file.txt")]
    [InlineData("folder//file.txt")]
    [InlineData("folder/file.txt/")]
    [InlineData("/rooted.txt")]
    [InlineData("C:\\rooted.txt")]
    [InlineData("\\\\server\\share\\file.txt")]
    public void ValidateEntry_RejectsNonCanonicalPortablePathStructure(string path)
    {
        var entry = Entry() with { RelativePath = path };
        Assert.Throws<InvalidDataException>(() => ManifestValidator.ValidateEntry(entry));
    }

    [Theory]
    [InlineData("folder\\file.txt")]
    [InlineData("report?.txt")]
    [InlineData("CON.txt")]
    [InlineData("name .txt")]
    [InlineData("Cafe\u0301.txt")]
    public void ValidateEntry_RejectsPathThatWouldChangeDuringSanitization(string path)
    {
        var entry = Entry() with { RelativePath = path };
        Assert.Throws<InvalidDataException>(() => ManifestValidator.ValidateEntry(entry));
    }

    [Fact]
    public void ValidateEntry_RejectsExcessivePathDepth()
    {
        var path = string.Join('/', Enumerable.Repeat("a", PortableRelativePath.MaximumSegments + 1));
        var entry = Entry() with { RelativePath = path };
        Assert.Throws<InvalidDataException>(() => ManifestValidator.ValidateEntry(entry));
    }

    private static FileManifestEntry Entry()
        => new(
            "file.txt",
            1,
            new string('0', 64),
            DateTimeOffset.UtcNow.AddMinutes(-1));
}
