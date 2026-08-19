using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class TransferStagingBudgetZeroAggregateTests
{
    [Fact]
    public void ZeroAggregateBudget_AllowsOnlyConfiguredZeroByteFileCount()
    {
        var budget = new TransferStagingBudget(2, 0, 0);

        Assert.Equal(0, budget.MaximumBytesForNextFile);
        budget.Commit(0);
        budget.Commit(0);

        Assert.Equal(2, budget.CommittedFiles);
        Assert.Equal(0, budget.CommittedBytes);
        Assert.Equal(0, budget.RemainingFiles);
        Assert.Throws<InvalidDataException>(() => budget.Commit(0));
        Assert.Throws<InvalidDataException>(() => budget.EnsureCanStage(1));
    }
}
