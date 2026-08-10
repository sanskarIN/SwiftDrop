using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class ExternalSharePackageValidatorTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_787_000_000);

    [Fact]
    public void Validate_AcceptsTextAndCanonicalFiles()
    {
        var manifest = Create(
            text: "hello",
            files:
            [
                new ExternalSharePackageFile("photo.jpg", 42),
                new ExternalSharePackageFile("notes.txt", 0)
            ]);

        Assert.Same(manifest, ExternalSharePackageValidator.Validate(manifest, Now));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG")]
    [InlineData("0123456789abcdef0123456789abcde-")]
    public void Validate_RejectsInvalidPackageId(string id)
        => Assert.Throws<InvalidDataException>(() =>
            ExternalSharePackageValidator.Validate(Create(packageId: id), Now));

    [Fact]
    public void Validate_RejectsExpiredPackage()
        => Assert.Throws<InvalidDataException>(() =>
            ExternalSharePackageValidator.Validate(
                Create(created: Now - ExternalSharePackageConstants.MaximumPackageAge - TimeSpan.FromSeconds(1)),
                Now));

    [Fact]
    public void Validate_RejectsExcessiveFutureClockSkew()
        => Assert.Throws<InvalidDataException>(() =>
            ExternalSharePackageValidator.Validate(
                Create(created: Now + ExternalSharePackageConstants.MaximumFutureClockSkew + TimeSpan.FromSeconds(1)),
                Now));

    [Fact]
    public void Validate_RejectsOversizedUtf8Text()
    {
        var text = new string('€', ProtocolConstants.MaxTextSnippetBytes / 3 + 1);
        Assert.Throws<InvalidDataException>(() =>
            ExternalSharePackageValidator.Validate(Create(text: text), Now));
    }

    [Fact]
    public void Validate_RejectsTooManyItems()
    {
        var files = Enumerable.Range(0, ExternalSharePackageConstants.MaximumItems + 1)
            .Select(i => new ExternalSharePackageFile($"{i}.txt", 1))
            .ToArray();
        Assert.Throws<InvalidDataException>(() =>
            ExternalSharePackageValidator.Validate(Create(files: files), Now));
    }

    [Theory]
    [InlineData("../escape.txt")]
    [InlineData("CON.txt")]
    [InlineData(" bad.txt ")]
    [InlineData("a?.txt")]
    public void Validate_RejectsNonCanonicalFileNames(string name)
        => Assert.ThrowsAny<Exception>(() =>
            ExternalSharePackageValidator.Validate(
                Create(files: [new ExternalSharePackageFile(name, 1)]),
                Now));

    [Fact]
    public void Validate_RejectsPortableCaseCollision()
        => Assert.Throws<InvalidDataException>(() =>
            ExternalSharePackageValidator.Validate(
                Create(files:
                [
                    new ExternalSharePackageFile("Readme.txt", 1),
                    new ExternalSharePackageFile("README.TXT", 1)
                ]),
                Now));

    [Fact]
    public void Validate_RejectsOversizedFile()
        => Assert.Throws<InvalidDataException>(() =>
            ExternalSharePackageValidator.Validate(
                Create(files: [new ExternalSharePackageFile("big.bin", ProtocolConstants.MaxSingleFileBytes + 1)]),
                Now));

    [Fact]
    public void Validate_RejectsEmptyPackage()
        => Assert.Throws<InvalidDataException>(() =>
            ExternalSharePackageValidator.Validate(Create(text: null, files: []), Now));

    private static ExternalSharePackageManifest Create(
        string packageId = "0123456789abcdef0123456789abcdef",
        DateTimeOffset? created = null,
        string? text = "hello",
        IReadOnlyList<ExternalSharePackageFile>? files = null)
        => new(
            ExternalSharePackageConstants.CurrentVersion,
            packageId,
            (created ?? Now).ToUnixTimeSeconds(),
            text,
            files ?? Array.Empty<ExternalSharePackageFile>());
}
