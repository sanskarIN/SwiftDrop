using System.Security.Cryptography.X509Certificates;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Networking;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.App.Services;

public sealed class ReceiveServerService : IAsyncDisposable
{
    private readonly TlsPeerServer _server;
    private readonly string _receiveRoot;
    private readonly Func<string, bool> _consumePairingNonce;
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;

    public ReceiveServerService(X509Certificate2 certificate, string receiveRoot, Func<string, bool> consumePairingNonce)
    {
        _receiveRoot = receiveRoot;
        _consumePairingNonce = consumePairingNonce;
        Directory.CreateDirectory(_receiveRoot);
        _server = new TlsPeerServer(new X509Certificate2(certificate.Export(X509ContentType.Pfx)), ProtocolConstants.DefaultPort);
    }

    public void Start()
    {
        _server.Start();
        _loop ??= Task.Run(() => LoopAsync(_cts.Token));
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var ssl = await _server.AcceptAsync(ct);
                _ = HandleAsync(ssl, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        }
    }

    private async Task HandleAsync(Stream connection, CancellationToken ct)
    {
        await using (connection)
        {
            try
            {
                var request = await FrameProtocol.ReadJsonAsync<IncomingRequest>(connection, ct);
                if (request.Type != "file" || request.ProtocolVersion != ProtocolConstants.CurrentVersion || !_consumePairingNonce(request.PairingNonce))
                {
                    await FrameProtocol.WriteJsonAsync(connection, new TransferResponse(false, 0, "Pairing authorization failed."), ct); return;
                }
                if (request.Entry.Length < 0 || request.Entry.Length > ProtocolConstants.MaxSingleFileBytes)
                {
                    await FrameProtocol.WriteJsonAsync(connection, new TransferResponse(false, 0, "Unsafe file size."), ct); return;
                }
                var final = PathGuard.ResolveUnderRoot(_receiveRoot, request.Entry.RelativePath);
                var partial = final + ".swiftdrop.part";
                var offset = File.Exists(partial) ? Math.Min(new FileInfo(partial).Length, request.Entry.Length) : 0;
                await FrameProtocol.WriteJsonAsync(connection, new TransferResponse(true, offset, null), ct);
                await new TransferEngine().ReceiveFileAsync(connection, _receiveRoot, request.Entry, offset, null, ct);
                await FrameProtocol.WriteJsonAsync(connection, new TransferResponse(true, request.Entry.Length, null), ct);
            }
            catch (Exception ex)
            {
                try { await FrameProtocol.WriteJsonAsync(connection, new TransferResponse(false, 0, ex.Message), CancellationToken.None); } catch { }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        await _server.DisposeAsync();
        if (_loop is not null) { try { await _loop; } catch (OperationCanceledException) { } }
        _cts.Dispose();
    }

    private sealed record IncomingRequest(string Type, string ProtocolVersion, string PairingNonce, FileManifestEntry Entry);
    private sealed record TransferResponse(bool Accepted, long ResumeOffset, string? Message);
}
