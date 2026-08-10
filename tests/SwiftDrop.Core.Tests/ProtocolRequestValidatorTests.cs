using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;

namespace SwiftDrop.Core.Tests;

public sealed class ProtocolRequestValidatorTests
{
    private const string Nonce = "ABCDEFGHIJKLMNOPQRSTUVWX";
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Validate_AcceptsFactoryRequests()
    {
        var entry = Entry("file.txt", 3, 'A');
        var file = ProtocolRequestFactory.CreateFile(Nonce, "id", "name", entry);
        var batch = ProtocolRequestFactory.CreateBatch(Nonce, "id", "name", "batch", [entry], 3);
        var text = ProtocolRequestFactory.CreateText(Nonce, "id", "name", "hello", Now.AddMinutes(1), Now);
        var pair = ProtocolRequestFactory.CreatePairRequest("id", "name", "12345678");

        Assert.Same(file, ProtocolRequestValidator.Validate(file, Now));
        Assert.Same(batch, ProtocolRequestValidator.Validate(batch, Now));
        Assert.Same(text, ProtocolRequestValidator.Validate(text, Now));
        Assert.Same(pair, ProtocolRequestValidator.Validate(pair, Now));
    }

    [Fact]
    public void Validate_RejectsCrossTypeFieldsOnFile()
    {
        var request = ProtocolRequestFactory.CreateFile(Nonce, "id", "name", Entry("a.txt", 1, 'A')) with
        {
            Text = "smuggled"
        };
        Assert.Throws<InvalidDataException>(() => ProtocolRequestValidator.Validate(request, Now));
    }

    [Fact]
    public void Validate_RejectsCrossTypeFieldsOnBatch()
    {
        var request = ProtocolRequestFactory.CreateBatch(
            Nonce, "id", "name", "batch", [Entry("a.txt", 1, 'A')], 1) with
        {
            PairingCode = "12345678"
        };
        Assert.Throws<InvalidDataException>(() => ProtocolRequestValidator.Validate(request, Now));
    }

    [Fact]
    public void Validate_RejectsCrossTypeFieldsOnText()
    {
        var request = ProtocolRequestFactory.CreateText(Nonce, "id", "name", "hello", Now.AddMinutes(1), Now) with
        {
            TransferId = "unexpected"
        };
        Assert.Throws<InvalidDataException>(() => ProtocolRequestValidator.Validate(request, Now));
    }

    [Fact]
    public void Validate_RejectsPairRequestWithTransferNonce()
    {
        var request = ProtocolRequestFactory.CreatePairRequest("id", "name") with
        {
            PairingNonce = Nonce
        };
        Assert.Throws<InvalidDataException>(() => ProtocolRequestValidator.Validate(request, Now));
    }

    [Fact]
    public void Validate_RejectsMalformedTextExpiry()
    {
        var request = new ProtocolRequest(
            "text", ProtocolConstants.CurrentVersion, Nonce, "id", "name",
            Text: "hello", ExpiresUnixSeconds: long.MaxValue);
        Assert.Throws<InvalidDataException>(() => ProtocolRequestValidator.Validate(request, Now));
    }

    [Fact]
    public void Validate_RejectsInvalidBatchDeclaredTotal()
    {
        var request = new ProtocolRequest(
            "batch", ProtocolConstants.CurrentVersion, Nonce, "id", "name",
            TransferId: "batch", Files: [Entry("a.txt", 2, 'A')], TotalBytes: 3);
        Assert.Throws<InvalidDataException>(() => ProtocolRequestValidator.Validate(request, Now));
    }

    [Fact]
    public void Validate_RejectsMissingFileEntry()
    {
        var request = new ProtocolRequest("file", ProtocolConstants.CurrentVersion, Nonce, "id", "name");
        Assert.Throws<InvalidDataException>(() => ProtocolRequestValidator.Validate(request, Now));
    }

    private static FileManifestEntry Entry(string path, long length, char hash)
        => new(path, length, new string(hash, 64), Now);
}
