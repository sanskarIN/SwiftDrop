using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class ProtocolSessionAuthorizerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private const string Nonce = "ABCDEFGHIJKLMNOPQRSTUVWX";

    [Fact]
    public void ValidateAndAuthorize_ConsumesTransferNonceExactlyOnce()
    {
        var store = new OneTimeAuthorizationStore();
        store.Register(Nonce, Now.AddMinutes(1), Now);
        var request = ProtocolRequestFactory.CreateFile(
            Nonce,
            "device",
            "Laptop",
            new FileManifestEntry("file.txt", 1, new string('A', 64), Now));

        Assert.Same(
            request,
            ProtocolSessionAuthorizer.ValidateAndAuthorize(request, Now, nonce => store.TryConsume(nonce, Now)));
        Assert.Throws<UnauthorizedAccessException>(() =>
            ProtocolSessionAuthorizer.ValidateAndAuthorize(request, Now, nonce => store.TryConsume(nonce, Now)));
    }

    [Fact]
    public void ValidateAndAuthorize_DoesNotConsumeAuthorizationForPairRequest()
    {
        var calls = 0;
        var request = ProtocolRequestFactory.CreatePairRequest("device", "Laptop", "12345678");

        Assert.Same(
            request,
            ProtocolSessionAuthorizer.ValidateAndAuthorize(
                request,
                Now,
                _ =>
                {
                    calls++;
                    return false;
                }));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void ValidateAndAuthorize_RejectsInvalidShapeBeforeConsumingNonce()
    {
        var calls = 0;
        var request = ProtocolRequestFactory.CreateFile(
            Nonce,
            "device",
            "Laptop",
            new FileManifestEntry("file.txt", 1, new string('A', 64), Now)) with
        {
            Text = "unexpected"
        };

        Assert.Throws<InvalidDataException>(() =>
            ProtocolSessionAuthorizer.ValidateAndAuthorize(
                request,
                Now,
                _ =>
                {
                    calls++;
                    return true;
                }));
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("../escape.txt")]
    [InlineData("folder//file.txt")]
    [InlineData("C:\\escape.txt")]
    public void ValidateAndAuthorize_RejectsUnsafeManifestPathBeforeConsumingNonce(string path)
    {
        var calls = 0;
        var valid = ProtocolRequestFactory.CreateFile(
            Nonce,
            "device",
            "Laptop",
            new FileManifestEntry("file.txt", 1, new string('A', 64), Now));
        var request = valid with { Entry = valid.Entry! with { RelativePath = path } };

        Assert.Throws<InvalidDataException>(() =>
            ProtocolSessionAuthorizer.ValidateAndAuthorize(
                request,
                Now,
                _ =>
                {
                    calls++;
                    return true;
                }));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void ValidateAndAuthorize_RejectsExpiredAuthorization()
    {
        var store = new OneTimeAuthorizationStore();
        store.Register(Nonce, Now.AddSeconds(1), Now);
        var request = ProtocolRequestFactory.CreateText(
            Nonce,
            "device",
            "Laptop",
            "hello",
            Now.AddMinutes(1),
            Now);

        Assert.Throws<UnauthorizedAccessException>(() =>
            ProtocolSessionAuthorizer.ValidateAndAuthorize(
                request,
                Now.AddSeconds(2),
                nonce => store.TryConsume(nonce, Now.AddSeconds(2))));
    }
}
