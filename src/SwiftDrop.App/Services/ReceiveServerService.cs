using System.Net.Security;
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
    private readonly Func<IncomingTransferPreview, CancellationToken, Task<bool>> _approveTransfer;
    private readonly Func<IncomingTransferPreview, string, bool, CancellationToken, Task>? _recordTransfer;
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;

    public ReceiveServerService(
        X509Certificate2 certificate,
        string receiveRoot,
        Func<string, bool> consumePairingNonce,
        Func<IncomingTransferPreview, CancellationToken, Task<bool>> approveTransfer,
        Func<IncomingTransferPreview, string, bool, CancellationToken, Task>? recordTransfer = null)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        ArgumentException.ThrowIfNullOrWhiteSpace(receiveRoot);
        _receiveRoot = receiveRoot;
        _consumePairingNonce = consumePairingNonce ?? throw new ArgumentNullException(nameof(consumePairingNonce));
        _approveTransfer = approveTransfer ?? throw new ArgumentNullException(nameof(approveTransfer));
        _recordTransfer = recordTransfer;
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
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                if (ct.IsCancellationRequested) break;
            }
        }
    }

    private async Task HandleAsync(SslStream connection, CancellationToken ct)
    {
        await using (connection)
        {
            IncomingTransferPreview? preview = null;
            try
            {
                var request = await FrameProtocol.ReadJsonAsync<IncomingRequest>(connection, ct);
                if (request.Type != "file" || request.ProtocolVersion != ProtocolConstants.CurrentVersion)
                {
                    await RejectAsync(connection, "Unsupported transfer request.", ct);
                    return;
                }
                if (!_consumePairingNonce(request.PairingNonce))
                {
                    await RejectAsync(connection, "Pairing authorization failed.", ct);
                    return;
                }
                if (request.Entry.Length < 0 || request.Entry.Length > ProtocolConstants.MaxSingleFileBytes)
                {
                    await RejectAsync(connection, "Unsafe file size.", ct);
                    return;
                }
                if (connection.RemoteCertificate is null)
                {
                    await RejectAsync(connection, "Sender certificate is required.", ct);
                    return;
                }

                using var senderCertificate = new X509Certificate2(connection.RemoteCertificate);
                preview = new IncomingTransferPreview(
                    request.SenderDeviceId,
                    request.SenderDeviceName,
                    Fingerprint.FromCertificate(senderCertificate),
                    request.Entry,
                    FileRiskClassifier.Classify(request.Entry.RelativePath));

                if (!await _approveTransfer(preview, ct))
                {
                    await RejectAsync(connection, "Receiver declined the transfer.", ct);
                    await RecordAsync(preview, "rejected", false, ct);
                    return;
                }

                var final = PathGuard.ResolveUnderRoot(_receiveRoot, request.Entry.RelativePath);
                var partial = final + ".swiftdrop.part";
                var offset = File.Exists(partial) ? Math.Min(new FileInfo(partial).Length, request.Entry.Length) : 0;
                await FrameProtocol.WriteJsonAsync(connection, new TransferResponse(true, offset, null), ct);
                await new TransferEngine().ReceiveFileAsync(connection, _receiveRoot, request.Entry, offset, null, ct);
                await FrameProtocol.WriteJsonAsync(connection, new TransferResponse(true, request.Entry.Length, null), ct);
                await RecordAsync(preview, "completed", true, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                if (preview is not null) await RecordBestEffortAsync(preview, "cancelled", false);
            }
            catch (Exception ex)
            {
                try
                {
                    await FrameProtocol.WriteJsonAsync(connection, new TransferResponse(false, 0, "Transfer failed safely."), CancellationToken.None);
                }
                catch
                {
                }
                if (preview is not null) await RecordBestEffortAsync(preview, "failed", false);
                _ = ex;
            }
        }
    }

    private static Task RejectAsync(Stream connection, string message, CancellationToken ct)
        => FrameProtocol.WriteJsonAsync(connection, new TransferResponse(false, 0, message), ct);

    private Task RecordAsync(IncomingTransferPreview preview, string status, bool verified, CancellationToken ct)
        => _recordTransfer?.Invoke(preview, status, verified, ct) ?? Task.CompletedTask;

    private async Task RecordBestEffortAsync(IncomingTransferPreview preview, string status, bool verified)
    {
        try
        {
            await RecordAsync(preview, status, verified, CancellationToken.None);
        }
        catch
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        await _server.DisposeAsync();
        if (_loop is not null)
        {
            try
            {
                await _loop;
            }
            catch (OperationCanceledException)
            {
            }
        }
        _cts.Dispose();
    }

    private sealed record IncomingRequest(
        string Type,
        string ProtocolVersion,
        string PairingNonce,
        string SenderDeviceId,
        string SenderDeviceName,
        FileManifestEntry Entry);

    private sealed record TransferResponse(bool Accepted, long ResumeOffset, string? Message);
}
