using SwiftDrop.Core.Models;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class ExternalSharePackageFileSetValidatorTests
{
    [Fact]
    public void ValidateExact_AcceptsExactSetRegardlessOfEnumerationOrder()
    {
        ExternalSharePackageFileSetValidator.ValidateExact(
            [
                new ExternalSharePackageFile("a.txt", 1),
                new ExternalSharePackageFile("photo.jpg", 2)
            ],
            ["photo.jpg", "a.txt"]);
    }

    [Fact]
    public void ValidateExact_RejectsUndeclaredExtraFile()
    {
        Assert.Throws<InvalidDataException>(() =>
            ExternalSharePackageFileSetValidator.ValidateExact(
                [new ExternalSharePackageFile("a.txt", 1)],
                ["a.txt", "extra.bin"]));
    }

    [Fact]
    public void ValidateExact_RejectsMissingDeclaredFile()
    {
        Assert.Throws<InvalidDataException>(() =>
            ExternalSharePackageFileSetValidator.ValidateExact(
                [
                    new ExternalSharePackageFile("a.txt", 1),
                    new ExternalSharePackageFile("b.txt", 1)
                ],
                ["a.txt"]));
    }

    [Fact]
    public void ValidateExact_RejectsNestedOrNonCanonicalNames()
    {
        Assert.Throws<InvalidDataException>(() =>
            ExternalSharePackageFileSetValidator.ValidateExact(
                [new ExternalSharePackageFile("folder/a.txt", 1)],
                ["folder/a.txt"]));

        Assert.Throws<InvalidDataException>(() =>
            ExternalSharePackageFileSetValidator.ValidateExact(
                [new ExternalSharePackageFile("a?.txt", 1)],
                ["a?.txt"]));
    }

    [Fact]
    public void ValidateExact_RejectsPortableDuplicateNames()
    {
        Assert.Throws<InvalidDataException>(() =>
            ExternalSharePackageFileSetValidator.ValidateExact(
                [
                    new ExternalSharePackageFile("A.txt", 1),
                    new ExternalSharePackageFile("a.txt", 1)
                ],
                ["A.txt", "a.txt"]));
    }

    [Fact]
    public void ValidateExact_AcceptsEmptyManifestAndEmptyDirectory()
    {
        ExternalSharePackageFileSetValidator.ValidateExact(
            Array.Empty<ExternalSharePackageFile>(),
            Array.Empty<string>());
    }
}
