using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class BatchManifestAggregateLimitTests
{
    [Fact]
    public void Validate_RejectsAggregateOverLimit_WhenEveryFileIsIndividuallyValid()
    {
        var files = Enumerable.Range(0, 11)
            .Select(i => new FileManifestEntry(
                $"file-{i}.bin",
                ProtocolConstants.MaxSingleFileBytes,
                new string('A', 64),
                DateTimeOffset.UtcNow.AddMinutes(-1)))
            .ToArray();
        var declared = checked(files.Sum(x => x.Length));

        Assert.True(files.All(x => x.Length <= ProtocolConstants.MaxSingleFileBytes));
        Assert.True(declared > ProtocolConstants.MaxBatchBytes);
        Assert.Throws<InvalidDataException>(() => BatchManifestValidator.Validate(files, declared));
    }
}
