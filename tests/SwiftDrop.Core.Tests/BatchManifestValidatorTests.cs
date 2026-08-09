using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class BatchManifestValidatorTests
{
    [Fact]
    public void Validate_AcceptsValidManifest()
    {
        var files = new[]
        {
            Entry("a.txt", 3),
            Entry("folder/b.txt", 7)
        };

        var result = BatchManifestValidator.Validate(files, 10);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Validate_RejectsDuplicatePaths()
    {
        var files = new[] { Entry("same.txt", 1), Entry("same.txt", 1) };
        Assert.Throws<InvalidDataException>(() => BatchManifestValidator.Validate(files, 2));
    }

    [Fact]
    public void Validate_RejectsCaseOnlyPortableCollision()
    {
        var files = new[] { Entry("Folder/Report.txt", 1), Entry("folder/report.TXT", 1) };
        Assert.Throws<InvalidDataException>(() => BatchManifestValidator.Validate(files, 2));
    }

    [Fact]
    public void Validate_RejectsUnicodeNormalizationCollision()
    {
        var files = new[] { Entry("Café.txt", 1), Entry("Cafe\u0301.txt", 1) };
        Assert.Throws<InvalidDataException>(() => BatchManifestValidator.Validate(files, 2));
    }

    [Fact]
    public void Validate_RejectsSanitizationCollision()
    {
        var files = new[] { Entry("report?.txt", 1), Entry("report*.txt", 1) };
        Assert.Throws<InvalidDataException>(() => BatchManifestValidator.Validate(files, 2));
    }

    [Fact]
    public void Validate_RejectsDeclaredTotalMismatch()
    {
        var files = new[] { Entry("a.txt", 5) };
        Assert.Throws<InvalidDataException>(() => BatchManifestValidator.Validate(files, 6));
    }

    [Fact]
    public void Validate_RejectsTotalAboveBatchLimit()
    {
        var files = new[]
        {
            Entry("a.bin", ProtocolConstants.MaxSingleFileBytes),
            Entry("b.bin", ProtocolConstants.MaxBatchBytes - ProtocolConstants.MaxSingleFileBytes + 1)
        };
        Assert.Throws<InvalidDataException>(() => BatchManifestValidator.Validate(files, ProtocolConstants.MaxBatchBytes + 1));
    }

    private static FileManifestEntry Entry(string path, long length)
        => new(path, length, new string('A', 64), DateTimeOffset.UtcNow.AddMinutes(-1));
}
