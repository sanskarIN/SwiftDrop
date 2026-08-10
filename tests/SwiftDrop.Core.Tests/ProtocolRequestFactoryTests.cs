using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;

namespace SwiftDrop.Core.Tests;

public sealed class ProtocolRequestFactoryTests
{
    private const string Nonce = "ABCDEFGHIJKLMNOPQRSTUVWX";
    private const string DeviceId = "device-1";
    private const string DeviceName = "Laptop";

    [Fact]
    public void CreateFile_ProducesCanonicalWireEnvelope()
    {
        var entry = new FileManifestEntry("hello.txt", 5, new string('A', 64), DateTimeOffset.UtcNow);
        var request = ProtocolRequestFactory.CreateFile(Nonce, DeviceId, DeviceName, entry);

        Assert.Equal("file", request.Type);
        Assert.Equal(ProtocolConstants.CurrentVersion, request.ProtocolVersion);
        Assert.Equal(Nonce, request.PairingNonce);
        Assert.Equal(entry, request.Entry);
        Assert.Null(request.Files);
        Assert.Null(request.Text);
    }

    [Fact]
    public void CreateBatch_ValidatesDeclaredTotalAndTransferId()
    {
        var files = new[]
        {
            new FileManifestEntry("a.txt", 2, new string('A', 64), DateTimeOffset.UtcNow),
            new FileManifestEntry("b.txt", 3, new string('B', 64), DateTimeOffset.UtcNow)
        };

        var request = ProtocolRequestFactory.CreateBatch(Nonce, DeviceId, DeviceName, "batch-1", files, 5);
        Assert.Equal("batch", request.Type);
        Assert.Equal(5, request.TotalBytes);
        Assert.Equal(2, request.Files!.Count);

        Assert.Throws<InvalidDataException>(() =>
            ProtocolRequestFactory.CreateBatch(Nonce, DeviceId, DeviceName, "batch-1", files, 6));
        Assert.Throws<InvalidDataException>(() =>
            ProtocolRequestFactory.CreateBatch(Nonce, DeviceId, DeviceName, "bad\n", files, 5));
    }

    [Fact]
    public void CreateText_UsesValidatedExpiryAndText()
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now + TimeSpan.FromMinutes(1);
        var request = ProtocolRequestFactory.CreateText(Nonce, DeviceId, DeviceName, "hello", expires, now);

        Assert.Equal("text", request.Type);
        Assert.Equal("hello", request.Text);
        Assert.Equal(expires.ToUnixTimeSeconds(), request.ExpiresUnixSeconds);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("ABCDEFGHIJKLMNOP!RSTUVWX")]
    [InlineData("ABCDEFGHIJKLMNOP QRSTUVWX")]
    public void CreateTransferRequest_RejectsInvalidNonce(string nonce)
    {
        var entry = new FileManifestEntry("hello.txt", 5, new string('A', 64), DateTimeOffset.UtcNow);
        Assert.ThrowsAny<Exception>(() =>
            ProtocolRequestFactory.CreateFile(nonce, DeviceId, DeviceName, entry));
    }

    [Fact]
    public void CreatePairRequest_RequiresExactlyEightDigitsWhenCodePresent()
    {
        var request = ProtocolRequestFactory.CreatePairRequest(DeviceId, DeviceName, "12345678");
        Assert.Equal("pair-request", request.Type);
        Assert.Equal("12345678", request.PairingCode);
        Assert.Null(request.PairingNonce);

        Assert.Throws<InvalidDataException>(() =>
            ProtocolRequestFactory.CreatePairRequest(DeviceId, DeviceName, "1234abcd"));
    }
}
