using SwiftDrop.Core.Protocol;

namespace SwiftDrop.Core.Tests;

public sealed class IncomingRequestPolicyTests
{
    [Theory]
    [InlineData("file")]
    [InlineData("batch")]
    [InlineData("text")]
    [InlineData("pair-request")]
    public void ValidateEnvelope_AcceptsKnownRequestTypes(string type)
        => IncomingRequestPolicy.ValidateEnvelope(ProtocolConstants.CurrentVersion, type);

    [Theory]
    [InlineData("")]
    [InlineData("FILE")]
    [InlineData("unknown")]
    public void ValidateEnvelope_RejectsUnknownRequestTypes(string type)
        => Assert.Throws<InvalidDataException>(() =>
            IncomingRequestPolicy.ValidateEnvelope(ProtocolConstants.CurrentVersion, type));

    [Fact]
    public void ValidateEnvelope_RejectsWrongVersion()
        => Assert.Throws<NotSupportedException>(() =>
            IncomingRequestPolicy.ValidateEnvelope("999", "file"));

    [Theory]
    [InlineData("", "Laptop")]
    [InlineData("device", "")]
    [InlineData("device\n", "Laptop")]
    [InlineData("device", "Laptop\n")]
    public void ValidateSenderIdentity_RejectsInvalidValues(string deviceId, string deviceName)
        => Assert.Throws<InvalidDataException>(() =>
            IncomingRequestPolicy.ValidateSenderIdentity(deviceId, deviceName));

    [Fact]
    public void ValidateSenderIdentity_RejectsOversizedValues()
    {
        Assert.Throws<InvalidDataException>(() =>
            IncomingRequestPolicy.ValidateSenderIdentity(new string('d', 129), "Laptop"));
        Assert.Throws<InvalidDataException>(() =>
            IncomingRequestPolicy.ValidateSenderIdentity("device", new string('n', 129)));
    }

    [Fact]
    public void ValidateSenderIdentity_AcceptsBoundedValues()
        => IncomingRequestPolicy.ValidateSenderIdentity("device-1", "Laptop");

    [Theory]
    [InlineData("")]
    [InlineData("\n")]
    [InlineData(" transfer")]
    [InlineData("transfer ")]
    [InlineData("transfer.id")]
    [InlineData("transfer/id")]
    [InlineData("transfer:id")]
    [InlineData("ट्रांसफर")]
    public void ValidateTransferId_RejectsInvalidIds(string transferId)
        => Assert.Throws<InvalidDataException>(() => IncomingRequestPolicy.ValidateTransferId(transferId));

    [Fact]
    public void ValidateTransferId_RejectsOversizedId()
        => Assert.Throws<InvalidDataException>(() =>
            IncomingRequestPolicy.ValidateTransferId(new string('x', 129)));

    [Theory]
    [InlineData("transfer-123")]
    [InlineData("transfer_123")]
    [InlineData("ABCdef0123456789")]
    public void ValidateTransferId_ReturnsValidTokenId(string transferId)
        => Assert.Equal(transferId, IncomingRequestPolicy.ValidateTransferId(transferId));

    [Fact]
    public void ValidateBatchItemStart_RejectsReorderedOrUnknownPath()
        => Assert.Throws<InvalidDataException>(() =>
            IncomingRequestPolicy.ValidateBatchItemStart("folder/a.txt", "folder/b.txt"));

    [Fact]
    public void ValidateBatchItemStart_AcceptsExactNegotiatedPath()
        => IncomingRequestPolicy.ValidateBatchItemStart("folder/a.txt", "folder/a.txt");
}
