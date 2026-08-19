using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class TransferStagingBudgetStateMachineTests
{
    [Fact]
    public void Budget_MatchesReferenceModelAcrossSeededOperations()
    {
        const int maximumFiles = 73;
        const long maximumAggregateBytes = 24_000;
        const long maximumSingleFileBytes = 1_200;
        const int operationCount = 5_000;

        var random = new Random(0x51A6E);
        var budget = new TransferStagingBudget(maximumFiles, maximumAggregateBytes, maximumSingleFileBytes);
        var committedFiles = 0;
        long committedBytes = 0;

        for (var operation = 0; operation < operationCount; operation++)
        {
            var remaining = Math.Max(0, maximumAggregateBytes - committedBytes);
            var length = PickLength(random, remaining, maximumSingleFileBytes);
            var canStage = length >= 0
                && length <= maximumSingleFileBytes
                && committedFiles < maximumFiles
                && committedBytes < maximumAggregateBytes
                && length <= remaining;

            if ((operation & 1) == 0)
            {
                if (canStage)
                {
                    budget.EnsureCanStage(length);
                }
                else
                {
                    Assert.Throws<InvalidDataException>(() => budget.EnsureCanStage(length));
                }
            }
            else if (canStage)
            {
                budget.Commit(length);
                committedFiles++;
                committedBytes += length;
            }
            else
            {
                Assert.Throws<InvalidDataException>(() => budget.Commit(length));
            }

            Assert.Equal(committedFiles, budget.CommittedFiles);
            Assert.Equal(committedBytes, budget.CommittedBytes);
            Assert.Equal(Math.Max(0, maximumFiles - committedFiles), budget.RemainingFiles);
            Assert.Equal(Math.Max(0, maximumAggregateBytes - committedBytes), budget.RemainingAggregateBytes);

            var expectedNext = committedFiles >= maximumFiles
                ? 0
                : Math.Min(maximumSingleFileBytes, Math.Max(0, maximumAggregateBytes - committedBytes));
            Assert.Equal(expectedNext, budget.MaximumBytesForNextFile);
        }
    }

    private static long PickLength(Random random, long remaining, long maximumSingleFileBytes)
    {
        return random.Next(8) switch
        {
            0 => -1,
            1 => 0,
            2 => maximumSingleFileBytes,
            3 => maximumSingleFileBytes + 1,
            4 => remaining,
            5 => remaining == long.MaxValue ? remaining : remaining + 1,
            _ => random.NextInt64(0, maximumSingleFileBytes + 2),
        };
    }
}
