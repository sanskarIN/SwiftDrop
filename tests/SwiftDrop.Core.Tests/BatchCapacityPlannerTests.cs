using SwiftDrop.Core.Models;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class BatchCapacityPlannerTests
{
    [Fact]
    public void CalculateRemainingBytes_SubtractsValidatedResumeOffsets()
    {
        var now = DateTimeOffset.UtcNow.AddMinutes(-1);
        var entries = new[]
        {
            Entry("a.bin", 100, now),
            Entry("b.bin", 200, now)
        };
        var offsets = new Dictionary<string, long>(StringComparer.Ordinal)
        {
            ["a.bin"] = 25,
            ["b.bin"] = 150
        };

        Assert.Equal(125, BatchCapacityPlanner.CalculateRemainingBytes(entries, offsets));
    }

    [Fact]
    public void CalculateRemainingBytes_RejectsOffsetBeyondFileLength()
    {
        var entries = new[] { Entry("a.bin", 100, DateTimeOffset.UtcNow.AddMinutes(-1)) };
        var offsets = new Dictionary<string, long> { ["a.bin"] = 101 };

        Assert.Throws<InvalidDataException>(() => BatchCapacityPlanner.CalculateRemainingBytes(entries, offsets));
    }

    private static FileManifestEntry Entry(string path, long length, DateTimeOffset timestamp)
        => new(path, length, new string('A', 64), timestamp);
}
