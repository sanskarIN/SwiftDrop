using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Networking;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;
using SwiftDrop.Core.Storage;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.App.Services;

public sealed class ReceiveServerService : IAsyncDisposable
{
    private readonly TlsPeerServer _server;
    private readonly string _receiveRoot;
    private readonly Func<string, bool> _consumePairingNonce;
    private readonly Func<IncomingTransferPreview, CancellationToken, Task<bool>> _approveTransfer;
    private readonly Func<IncomingTransferPreview, string, bool, CancellationToken, Task>? _recordTransfer;
    private readonly Func<IncomingTextPreview, CancellationToken, Task<IncomingTextDecision>>? _approveText;
    private readonly Func<IncomingTextPreview, string, CancellationToken, Task>? _recordText;
    private readonly AttemptRateLimiter _pairingAttemptLimiter = new(8, TimeSpan.FromMinutes(1));
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;

    public ReceiveServerService(
        X509Certificate2 certificate,
        string receiveRoot,
        Func<string, bool> consumePairingNonce,
        Func<IncomingTransferPreview, CancellationToken, Task<bool>> approveTransfer,
        Func<IncomingTransferPreview, string, bool, CancellationToken, Task>? recordTransfer = null,
        Func<IncomingTextPreview, CancellationToken, Task<IncomingTextDecision>>? approveText = null,
        Func<IncomingTextPreview, string, CancellationToken, Task>? recordText = null)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        ArgumentException.ThrowIfNullOrWhiteSpace(receiveRoot);
        _receiveRoot = receiveRoot;
        _consumePairingNonce = consumePairingNonce ?? throw new ArgumentNullException(nameof(consumePairingNonce));
        _approveTransfer = approveTransfer ?? throw new ArgumentNullException(nameof(approveTransfer));
        _recordTransfer = recordTransfer;
        _approveText = approveText;
        _recordText = recordText;
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
            IncomingTransferPreview? filePreview = null;
            IncomingTextPreview? textPreview = null;
            try
            {
                var request = await FrameProtocol.ReadJsonAsync<IncomingRequest>(connection, ct);
                if (request.ProtocolVersion != ProtocolConstants.CurrentVersion || request.Type is not ("file" or "text"))
                {
                    await RejectAsync(connection, "Unsupported transfer request.", ct);
                    return;
                }
                if (connection.RemoteCertificate is null)
                {
                    await RejectAsync(connection, "Sender certificate is required.", ct);
                    return;
                }

                using var senderCertificate = new X509Certificate2(connection.RemoteCertificate);
                var senderFingerprint = Fingerprint.FromCertificate(senderCertificate);
                if (!_pairingAttemptLimiter.TryAcquire(senderFingerprint, DateTimeOffset.UtcNow))
                {
                    await RejectAsync(connection, "Too many pairing attempts. Try again shortly.", ct);
                    return;
                }
                if (!_consumePairingNonce(request.PairingNonce))
                {
                    await RejectAsync(connection, "Pairing authorization failed.", ct);
                    return;
                }

                if (request.Type == "text")
                {
                    textPreview = await HandleTextAsync(connection, request, senderFingerprint, ct);
                    return;
                }

                if (request.Entry is null)
                {
                    await RejectAsync(connection, "File metadata is required.", ct);
                    return;
                }
                if (request.Entry.Length < 0 || request.Entry.Length > ProtocolConstants.MaxSingleFileBytes)
                {
                    await RejectAsync(connection, "Unsafe file size.", ct);
                    return;
                }

                filePreview = new IncomingTransferPreview(
                    request.SenderDeviceId,
                    request.SenderDeviceName,
                    senderFingerprint,
                    request.Entry,
                    FileRiskClassifier.Classify(request.Entry.RelativePath));

                if (!await _approveTransfer(filePreview, ct))
                {
                    await RejectAsync(connection, "Receiver declined the transfer.", ct);
                    await RecordAsync(filePreview, "rejected", false, ct);
                    return;
                }

                var requestedFinal = PathGuard.ResolveUnderRoot(_receiveRoot, request.Entry.RelativePath);
                var final = File.Exists(requestedFinal) || Directory.Exists(requestedFinal)
                    ? PathGuard.GetCollisionFreePath(requestedFinal)
                    : requestedFinal;
                var effectiveRelativePath = Path.GetRelativePath(_receiveRoot, final);
                var effectiveEntry = request.Entry with { RelativePath = effectiveRelativePath };
                var partial = final + ".swiftdrop.part";
                var offset = File.Exists(partial) ? Math.Min(new FileInfo(partial).Length, effectiveEntry.Length) : 0;
                StorageCapacityGuard.EnsureCapacity(final, effectiveEntry.Length - offset);
                await FrameProtocol.WriteJsonAsync(connection, new TransferResponse(true, offset, null), ct);
                await new TransferEngine().ReceiveFileAsync(connection, _receiveRoot, effectiveEntry, offset, null, ct);
                await FrameProtocol.WriteJsonAsync(connection, new TransferResponse(true, effectiveEntry.Length, null), ct);
                await RecordAsync(filePreview with { Entry = effectiveEntry }, "completed", true, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                if (filePreview is not null) await RecordBestEffortAsync(filePreview, "cancelled", false);
                if (textPreview is not null) await RecordTextBestEffortAsync(textPreview, "cancelled");
            }
            catch (Exception)
            {
                try
                {
                    await FrameProtocol.WriteJsonAsync(connection, new TransferResponse(false, 0, "Transfer failed safely."), CancellationToken.None);
                }
                catch
                {
                }
                if (filePreview is not null) await RecordBestEffortAsync(filePreview, "failed", false);
                if (textPreview is not null) await RecordTextBestEffortAsync(textPreview, "failed");
            }
        }
    }

    private async Task<IncomingTextPreview> HandleTextAsync(
        Stream connection,
        IncomingRequest request,
        string senderFingerprint,
        CancellationToken ct)
    {
        if (_approveText is null)
        {
            await RejectAsync(connection, "Text receiving is unavailable.", ct);
            throw new InvalidOperationException("Text receiving is unavailable.");
        }
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            await RejectAsync(connection, "Text snippet is empty.", ct);
            throw new InvalidDataException("Text snippet is empty.");
        }
        if (Encoding.UTF8.GetByteCount(request.Text) > ProtocolConstants.MaxTextBytes)
        {
            await RejectAsync(connection, "Text snippet is too large.", ct);
            throw new InvalidDataException("Text snippet is too large.");
        }
        var expiresUtc = DateTimeOffset.FromUnixTimeSeconds(request.ExpiresUnixSeconds ?? 0);
        if (expiresUtc <= DateTimeOffset.UtcNow || expiresUtc > DateTimeOffset.UtcNow.Add(ProtocolConstants.TextSnippetLifetime).AddSeconds(30))
        {
            await RejectAsync(connection, "Text snippet has expired or has an invalid expiry.", ct);
            throw new InvalidDataException("Text snippet expiry is invalid.");
        }

        var preview = new IncomingTextPreview(
            request.SenderDeviceId,
            request.SenderDeviceName,
            senderFingerprint,
            request.Text,
            expiresUtc);
        var decision = await _approveText(preview, ct);
        if (decision == IncomingTextDecision.Reject)
        {
            await RejectAsync(connection, "Receiver declined the text snippet.", ct);
            await RecordTextAsync(preview, "rejected", ct);
            return preview;
        }

        await FrameProtocol.WriteJsonAsync(connection, new TransferResponse(true, 0, null), ct);
        await RecordTextAsync(preview, decision == IncomingTextDecision.AcceptAndCopy ? "copied" : "accepted", ct);
        return preview;
    }

    private static Task RejectAsync(Stream connection, string message, CancellationToken ct)
        => FrameProtocol.WriteJsonAsync(connection, new TransferResponse(false, 0, message), ct);

    private Task RecordAsync(IncomingTransferPreview preview, string status, bool verified, CancellationToken ct)
        => _recordTransfer?.Invoke(preview, status, verified, ct) ?? Task.CompletedTask;

    private Task RecordTextAsync(IncomingTextPreview preview, string status, CancellationToken ct)
        => _recordText?.Invoke(preview, status, ct) ?? Task.CompletedTask;

    private async Task RecordBestEffortAsync(IncomingTransferPreview preview, string status, bool verified)
    {
        try { await RecordAsync(preview, status, verified, CancellationToken.None); } catch { }
    }

    private async Task RecordTextBestEffortAsync(IncomingTextPreview preview, string status)
    {
        try { await RecordTextAsync(preview, status, CancellationToken.None); } catch { }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        await _server.DisposeAsync();
        if (_loop is not null)
        {
            try { await _loop; } catch (OperationCanceledException) { }
        }
        _cts.Dispose();
    }

    private sealed record IncomingRequest(
        string Type,
        string ProtocolVersion,
        string PairingNonce,
        string SenderDeviceId,
        string SenderDeviceName,
        FileManifestEntry? Entry,
        string? Text,
        long? ExpiresUnixSeconds);

    private sealed record TransferResponse(bool Accepted, long ResumeOffset, string? Message);
}
