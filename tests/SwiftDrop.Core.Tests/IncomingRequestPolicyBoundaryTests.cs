using SwiftDrop.Core.Protocol;

namespace SwiftDrop.Core.Tests;

public sealed class IncomingRequestPolicyBoundaryTests
{
    [Fact]
    public void SenderIdentity_AcceptsExactMaximumLengths()
    {
        var deviceId = new string('d', 128);
        var deviceName = new string('n', 128);

        IncomingRequestPolicy.ValidateSenderIdentity(deviceId, deviceName);
    }

    [Fact]
    public void SenderIdentity_AcceptsUnicodeDisplayNameWithoutControls()
        => IncomingRequestPolicy.ValidateSenderIdentity(
            "device-हिन्दी-1",
            "मेरे फ़ोन का SwiftDrop");

    [Theory]
    [InlineData("\t")]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\0")]
    public void SenderIdentity_RejectsControlCharacters(string control)
    {
        Assert.Throws<InvalidDataException>(() =>
            IncomingRequestPolicy.ValidateSenderIdentity("device" + control, "Laptop"));
        Assert.Throws<InvalidDataException>(() =>
            IncomingRequestPolicy.ValidateSenderIdentity("device", "Laptop" + control));
    }

    [Fact]
    public void TransferId_AcceptsExactMaximumLength()
    {
        var id = new string('t', 128);
        Assert.Equal(id, IncomingRequestPolicy.ValidateTransferId(id));
    }

    [Fact]
    public void TransferId_RejectsOneCharacterBeyondMaximum()
        => Assert.Throws<InvalidDataException>(() =>
            IncomingRequestPolicy.ValidateTransferId(new string('t', 129)));

    [Fact]
    public void TransferId_RejectsEmbeddedControlCharacter()
        => Assert.Throws<InvalidDataException>(() =>
            IncomingRequestPolicy.ValidateTransferId("transfer\n123"));

    [Fact]
    public void BatchItemStart_IsOrdinalAndCaseSensitive()
    {
        IncomingRequestPolicy.ValidateBatchItemStart("Folder/File.txt", "Folder/File.txt");
        Assert.Throws<InvalidDataException>(() =>
            IncomingRequestPolicy.ValidateBatchItemStart("Folder/File.txt", "folder/file.txt"));
    }
}
