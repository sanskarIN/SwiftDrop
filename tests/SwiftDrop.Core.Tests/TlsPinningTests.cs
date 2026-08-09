using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using SwiftDrop.Core.Networking;
using SwiftDrop.Core.Security;
using Xunit;

namespace SwiftDrop.Core.Tests;

public sealed class TlsPinningTests
{
    [Fact]
    public async Task ConnectAsync_AcceptsExactServerFingerprint()
    {
        using var serverCertificate = new CertificateService().CreateSelfSigned("pin-server");
        using var clientCertificate = new CertificateService().CreateSelfSigned("pin-client");
        var port = GetAvailablePort();
        await using var server = new TlsPeerServer(serverCertificate, port);
        server.Start();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var acceptTask = server.AcceptAsync(timeout.Token);
        await using var client = await new TlsPeerClient().ConnectAsync(
            "127.0.0.1",
            port,
            Fingerprint.FromCertificate(serverCertificate),
            clientCertificate,
            timeout.Token);
        await using var accepted = await acceptTask;

        Assert.True(client.IsAuthenticated);
        Assert.True(client.IsEncrypted);
        Assert.True(accepted.IsAuthenticated);
        Assert.True(accepted.IsEncrypted);
        Assert.NotNull(accepted.RemoteCertificate);
    }

    [Fact]
    public async Task ConnectAsync_RejectsWrongServerFingerprint()
    {
        using var serverCertificate = new CertificateService().CreateSelfSigned("wrong-pin-server");
        using var otherCertificate = new CertificateService().CreateSelfSigned("unrelated-server");
        using var clientCertificate = new CertificateService().CreateSelfSigned("wrong-pin-client");
        var port = GetAvailablePort();
        await using var server = new TlsPeerServer(serverCertificate, port);
        server.Start();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var acceptTask = server.AcceptAsync(timeout.Token);
        await Assert.ThrowsAnyAsync<AuthenticationException>(async () =>
        {
            await using var _ = await new TlsPeerClient().ConnectAsync(
                "127.0.0.1",
                port,
                Fingerprint.FromCertificate(otherCertificate),
                clientCertificate,
                timeout.Token);
        });

        try
        {
            await acceptTask;
        }
        catch (Exception ex) when (ex is AuthenticationException or IOException or OperationCanceledException)
        {
        }
    }

    [Fact]
    public async Task Bootstrap_CapturesObservedServerFingerprint()
    {
        using var serverCertificate = new CertificateService().CreateSelfSigned("bootstrap-server");
        using var clientCertificate = new CertificateService().CreateSelfSigned("bootstrap-client");
        var expected = Fingerprint.FromCertificate(serverCertificate);
        var port = GetAvailablePort();
        await using var server = new TlsPeerServer(serverCertificate, port);
        server.Start();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var acceptTask = server.AcceptAsync(timeout.Token);
        await using var bootstrap = await new TlsPeerClient().ConnectUnpinnedBootstrapAsync(
            "127.0.0.1",
            port,
            clientCertificate,
            timeout.Token);
        await using var accepted = await acceptTask;

        Assert.True(Fingerprint.FixedTimeEquals(expected, bootstrap.ServerFingerprint));
        Assert.True(bootstrap.Stream.IsEncrypted);
        Assert.NotNull(accepted.RemoteCertificate);
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
