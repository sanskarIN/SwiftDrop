using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Networking;

public sealed class TlsPeerClient
{
    public async Task<SslStream> ConnectAsync(string host, int port, string expectedFingerprint, CancellationToken ct)
    {
        var tcp = new TcpClient();
        try
        {
            await tcp.ConnectAsync(host, port, ct);
            var ssl = new SslStream(tcp.GetStream(), false, (_, cert, _, errors) =>
            {
                if (cert is null) return false;
                using var x509 = new X509Certificate2(cert);
                return Fingerprint.FixedTimeEquals(expectedFingerprint, Fingerprint.FromCertificate(x509));
            });
            await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = "swiftdrop-peer",
                EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck
            }, ct);
            return ssl;
        }
        catch
        {
            tcp.Dispose();
            throw;
        }
    }
}
