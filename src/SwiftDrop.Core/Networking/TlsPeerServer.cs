using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace SwiftDrop.Core.Networking;

public sealed class TlsPeerServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly X509Certificate2 _certificate;
    private readonly CancellationTokenSource _cts = new();

    public TlsPeerServer(X509Certificate2 certificate, int port)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        _certificate = certificate;
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public void Start() => _listener.Start();

    public async Task<SslStream> AcceptAsync(CancellationToken ct)
        => (await AcceptConnectionAsync(ct)).Stream;

    public async Task<TlsAcceptedConnection> AcceptConnectionAsync(CancellationToken ct)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
        var client = await _listener.AcceptTcpClientAsync(linked.Token);
        var remoteAddress = (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "unknown";
        var ssl = new SslStream(client.GetStream(), false);
        try
        {
            await ssl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
            {
                ServerCertificate = _certificate,
                ClientCertificateRequired = true,
                EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                RemoteCertificateValidationCallback = (_, certificate, _, _) => certificate is not null
            }, linked.Token);
            if (ssl.RemoteCertificate is null)
                throw new AuthenticationException("SwiftDrop requires a sender certificate.");
            return new TlsAcceptedConnection(ssl, remoteAddress);
        }
        catch
        {
            ssl.Dispose();
            client.Dispose();
            throw;
        }
    }

    public ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _listener.Stop();
        _cts.Dispose();
        _certificate.Dispose();
        return ValueTask.CompletedTask;
    }
}

public sealed record TlsAcceptedConnection(SslStream Stream, string RemoteAddress);
