using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class TransferStagingBudgetTests
{
    [Fact]
    public void Budget_TracksCommittedFilesAndBytes()
    {
        var budget = new TransferStagingBudget(3, 100, 60);

        budget.EnsureCanStage(40);
        Assert.Equal(0, budget.CommittedFiles);
        Assert.Equal(0, budget.CommittedBytes);
        budget.Commit(40);

        Assert.Equal(1, budget.CommittedFiles);
        Assert.Equal(40, budget.CommittedBytes);
        Assert.Equal(2, budget.RemainingFiles);
        Assert.Equal(60, budget.RemainingAggregateBytes);
        Assert.Equal(60, budget.MaximumBytesForNextFile);
    }

    [Fact]
    public void Budget_RejectsPerFileAndAggregateOverflow()
    {
        var budget = new TransferStagingBudget(3, 100, 60);
        Assert.Throws<InvalidDataException>(() => budget.EnsureCanStage(61));

        budget.Commit(60);
        Assert.Throws<InvalidDataException>(() => budget.EnsureCanStage(41));
        budget.Commit(40);

        Assert.Equal(0, budget.MaximumBytesForNextFile);
        Assert.Throws<InvalidDataException>(() => budget.EnsureCanStage(0));
    }

    [Fact]
    public void Budget_RejectsFileCountOverflowIncludingZeroByteFiles()
    {
        var budget = new TransferStagingBudget(2, 100, 100);
        budget.Commit(0);
        budget.Commit(0);

        Assert.Equal(0, budget.RemainingFiles);
        Assert.Throws<InvalidDataException>(() => budget.Commit(0));
    }

    [Fact]
    public void EnsureCanStage_DoesNotConsumeBudgetWhenCopyHasNotCommitted()
    {
        var budget = new TransferStagingBudget(1, 10, 10);
        budget.EnsureCanStage(10);
        budget.EnsureCanStage(10);

        Assert.Equal(1, budget.RemainingFiles);
        Assert.Equal(10, budget.RemainingAggregateBytes);
    }

    [Theory]
    [InlineData(0, 10, 10)]
    [InlineData(1, -1, 0)]
    [InlineData(1, 10, -1)]
    [InlineData(1, 10, 11)]
    public void Constructor_RejectsInvalidLimits(int files, long aggregate, long single)
        => Assert.ThrowsAny<ArgumentException>(() => new TransferStagingBudget(files, aggregate, single));
}
