using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Networking;

public sealed class TlsPeerClient
{
    public async Task<SslStream> ConnectAsync(
        string host,
        int port,
        string expectedFingerprint,
        X509Certificate2 clientCertificate,
        CancellationToken ct)
    {
        ValidateArguments(host, port, clientCertificate);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedFingerprint);

        var tcp = new TcpClient();
        try
        {
            await tcp.ConnectAsync(host, port, ct);
            var ssl = new SslStream(tcp.GetStream(), false, (_, cert, _, _) =>
            {
                if (cert is null) return false;
                using var x509 = X509CertificateLoader.LoadCertificate(cert.GetRawCertData());
                return Fingerprint.FixedTimeEquals(expectedFingerprint, Fingerprint.FromCertificate(x509));
            });
            await AuthenticateAsync(ssl, clientCertificate, ct);
            return ssl;
        }
        catch
        {
            tcp.Dispose();
            throw;
        }
    }

    public async Task<BootstrapTlsConnection> ConnectUnpinnedBootstrapAsync(
        string host,
        int port,
        X509Certificate2 clientCertificate,
        CancellationToken ct)
    {
        ValidateArguments(host, port, clientCertificate);

        var tcp = new TcpClient();
        try
        {
            await tcp.ConnectAsync(host, port, ct);
            var ssl = new SslStream(tcp.GetStream(), false, (_, cert, _, _) => cert is not null);
            await AuthenticateAsync(ssl, clientCertificate, ct);
            if (ssl.RemoteCertificate is null)
                throw new AuthenticationException("Manual pairing bootstrap did not receive a server certificate.");

            using var remote = X509CertificateLoader.LoadCertificate(ssl.RemoteCertificate.GetRawCertData());
            return new BootstrapTlsConnection(ssl, Fingerprint.FromCertificate(remote));
        }
        catch
        {
            tcp.Dispose();
            throw;
        }
    }

    private static Task AuthenticateAsync(SslStream ssl, X509Certificate2 clientCertificate, CancellationToken ct)
        => ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
        {
            TargetHost = "swiftdrop-peer",
            EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12,
            CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
            ClientCertificates = new X509CertificateCollection { clientCertificate }
        }, ct);

    private static void ValidateArguments(string host, int port, X509Certificate2 clientCertificate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentNullException.ThrowIfNull(clientCertificate);
        if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
    }
}

public sealed class BootstrapTlsConnection : IAsyncDisposable
{
    public BootstrapTlsConnection(SslStream stream, string serverFingerprint)
    {
        Stream = stream;
        ServerFingerprint = serverFingerprint;
    }

    public SslStream Stream { get; }
    public string ServerFingerprint { get; }

    public ValueTask DisposeAsync()
    {
        Stream.Dispose();
        return ValueTask.CompletedTask;
    }
}
