using SwiftDrop.Core.Protocol;

namespace SwiftDrop.Core.Tests;

public sealed class TransferResponsePolicyTests
{
    [Theory]
    [InlineData(0, 10)]
    [InlineData(5, 10)]
    [InlineData(10, 10)]
    public void ValidateResumeOffset_AcceptsBoundedOffsets(long offset, long length)
        => Assert.Equal(offset, TransferResponsePolicy.ValidateResumeOffset(true, offset, length, null));

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(11, 10)]
    public void ValidateResumeOffset_RejectsOutOfRangeOffsets(long offset, long length)
        => Assert.Throws<InvalidDataException>(() =>
            TransferResponsePolicy.ValidateResumeOffset(true, offset, length, null));

    [Fact]
    public void ValidateResumeOffset_PropagatesReceiverRejection()
        => Assert.Throws<IOException>(() =>
            TransferResponsePolicy.ValidateResumeOffset(false, 0, 10, "declined"));

    [Fact]
    public void ValidateCompletion_AcceptsExactLength()
        => TransferResponsePolicy.ValidateCompletion(true, 10, 10, null);

    [Theory]
    [InlineData(9, 10)]
    [InlineData(11, 10)]
    public void ValidateCompletion_RejectsMismatchedLength(long completed, long expected)
        => Assert.Throws<InvalidDataException>(() =>
            TransferResponsePolicy.ValidateCompletion(true, completed, expected, null));

    [Fact]
    public void ValidateCompletion_RejectsReceiverFailure()
        => Assert.Throws<IOException>(() =>
            TransferResponsePolicy.ValidateCompletion(false, 0, 10, "failed"));

    [Fact]
    public void ValidateTextAcknowledgement_AcceptsZeroOffset()
        => TransferResponsePolicy.ValidateTextAcknowledgement(true, 0, null);

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(long.MaxValue)]
    public void ValidateTextAcknowledgement_RejectsNonzeroOffset(long offset)
        => Assert.Throws<InvalidDataException>(() =>
            TransferResponsePolicy.ValidateTextAcknowledgement(true, offset, null));
}
