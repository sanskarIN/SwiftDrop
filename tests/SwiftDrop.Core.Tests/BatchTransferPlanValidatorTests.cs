using SwiftDrop.Core.Models;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class BatchTransferPlanValidatorTests
{
    [Fact]
    public void Validate_AcceptsCompleteSelectivePlan()
    {
        var sources = new[] { Entry("a.txt", 10), Entry("b.txt", 20) };
        var response = new BatchTransferResponse(true, new[]
        {
            new BatchItemPlan("a.txt", 4, true),
            new BatchItemPlan("b.txt", 0, false)
        });

        var plans = BatchTransferPlanValidator.Validate(sources, response);

        Assert.Equal(2, plans.Count);
        Assert.True(plans["a.txt"].Accepted);
        Assert.False(plans["b.txt"].Accepted);
    }

    [Fact]
    public void Validate_RejectsUnknownPath()
    {
        var sources = new[] { Entry("a.txt", 10) };
        var response = new BatchTransferResponse(true, new[] { new BatchItemPlan("other.txt", 0, true) });
        Assert.Throws<InvalidDataException>(() => BatchTransferPlanValidator.Validate(sources, response));
    }

    [Fact]
    public void Validate_RejectsDuplicatePath()
    {
        var sources = new[] { Entry("a.txt", 10), Entry("b.txt", 20) };
        var response = new BatchTransferResponse(true, new[]
        {
            new BatchItemPlan("a.txt", 0, true),
            new BatchItemPlan("a.txt", 0, false)
        });
        Assert.Throws<InvalidDataException>(() => BatchTransferPlanValidator.Validate(sources, response));
    }

    [Fact]
    public void Validate_RejectsMissingPlanInAcceptedResponse()
    {
        var sources = new[] { Entry("a.txt", 10), Entry("b.txt", 20) };
        var response = new BatchTransferResponse(true, new[] { new BatchItemPlan("a.txt", 0, true) });
        Assert.Throws<InvalidDataException>(() => BatchTransferPlanValidator.Validate(sources, response));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void Validate_RejectsOutOfRangeResumeOffset(long offset)
    {
        var sources = new[] { Entry("a.txt", 10) };
        var response = new BatchTransferResponse(true, new[] { new BatchItemPlan("a.txt", offset, true) });
        Assert.Throws<InvalidDataException>(() => BatchTransferPlanValidator.Validate(sources, response));
    }

    [Fact]
    public void Validate_RejectsResumeOffsetOnRejectedItem()
    {
        var sources = new[] { Entry("a.txt", 10) };
        var response = new BatchTransferResponse(false, new[] { new BatchItemPlan("a.txt", 2, false) });
        Assert.Throws<InvalidDataException>(() => BatchTransferPlanValidator.Validate(sources, response));
    }

    [Fact]
    public void Validate_RejectsAcceptedItemWhenOverallResponseRejected()
    {
        var sources = new[] { Entry("a.txt", 10) };
        var response = new BatchTransferResponse(false, new[] { new BatchItemPlan("a.txt", 0, true) });
        Assert.Throws<InvalidDataException>(() => BatchTransferPlanValidator.Validate(sources, response));
    }

    [Fact]
    public void Validate_RejectsAcceptedResponseWithNoAcceptedFiles()
    {
        var sources = new[] { Entry("a.txt", 10) };
        var response = new BatchTransferResponse(true, new[] { new BatchItemPlan("a.txt", 0, false) });
        Assert.Throws<InvalidDataException>(() => BatchTransferPlanValidator.Validate(sources, response));
    }

    private static FileManifestEntry Entry(string path, long length)
        => new(path, length, new string('A', 64), DateTimeOffset.UtcNow.AddMinutes(-1));
}
