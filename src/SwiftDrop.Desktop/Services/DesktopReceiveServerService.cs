using System.Diagnostics;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Networking;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;
using SwiftDrop.Core.Storage;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Desktop.Services;

public sealed class DesktopReceiveServerService : IAsyncDisposable
{
    private readonly TlsPeerServer _server;
    private readonly string _receiveRoot;
    private readonly DesktopBatchResumeStateService _batchResumeState;
    private readonly Func<string, bool> _consumePairingNonce;
    private readonly Func<DesktopIncomingTransferPreview, CancellationToken, Task<bool>> _approveTransfer;
    private readonly Func<DesktopIncomingTransferPreview, string, bool, TimeSpan?, long?, CancellationToken, Task>? _recordTransfer;
    private readonly Func<DesktopIncomingBatchPreview, CancellationToken, Task<DesktopIncomingBatchDecision>>? _approveBatch;
    private readonly Func<DesktopIncomingTextPreview, CancellationToken, Task<DesktopIncomingTextDecision>>? _approveText;
    private readonly Func<DesktopIncomingTextPreview, string, CancellationToken, Task>? _recordText;
    private readonly Func<DesktopIncomingPairingRequest, CancellationToken, Task<bool>>? _approvePairing;
    private readonly Func<string>? _createPairingLink;
    private readonly Func<string?, bool>? _consumePairingCode;
    private readonly AttemptRateLimiter _pairingAttemptLimiter = new(8, TimeSpan.FromMinutes(1));
    private readonly DestinationReservationSet _destinationReservations = new();
    private readonly AsyncSessionTracker _activeSessions = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;
    private int _started;
    private int _disposed;

    public DesktopReceiveServerService(
        X509Certificate2 certificate,
        string receiveRoot,
        Func<string, bool> consumePairingNonce,
        Func<DesktopIncomingTransferPreview, CancellationToken, Task<bool>> approveTransfer,
        Func<DesktopIncomingTransferPreview, string, bool, TimeSpan?, long?, CancellationToken, Task>? recordTransfer = null,
        Func<DesktopIncomingTextPreview, CancellationToken, Task<DesktopIncomingTextDecision>>? approveText = null,
        Func<DesktopIncomingTextPreview, string, CancellationToken, Task>? recordText = null,
        Func<DesktopIncomingPairingRequest, CancellationToken, Task<bool>>? approvePairing = null,
        Func<string>? createPairingLink = null,
        Func<string?, bool>? consumePairingCode = null,
        Func<DesktopIncomingBatchPreview, CancellationToken, Task<DesktopIncomingBatchDecision>>? approveBatch = null)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        ArgumentException.ThrowIfNullOrWhiteSpace(receiveRoot);
        _receiveRoot = Path.GetFullPath(receiveRoot);
        _batchResumeState = new DesktopBatchResumeStateService(_receiveRoot);
        _consumePairingNonce = consumePairingNonce ?? throw new ArgumentNullException(nameof(consumePairingNonce));
        _approveTransfer = approveTransfer ?? throw new ArgumentNullException(nameof(approveTransfer));
        _recordTransfer = recordTransfer;
        _approveBatch = approveBatch;
        _approveText = approveText;
        _recordText = recordText;
        _approvePairing = approvePairing;
        _createPairingLink = createPairingLink;
        _consumePairingCode = consumePairingCode;
        Directory.CreateDirectory(_receiveRoot);
        _server = new TlsPeerServer(CopyCertificateWithPrivateKey(certificate), ProtocolConstants.DefaultPort);
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (Interlocked.CompareExchange(ref _started, 1, 0) != 0) return;
        try
        {
            _server.Start();
            _loop = Task.Run(() => LoopAsync(_cts.Token));
        }
        catch
        {
            Interlocked.Exchange(ref _started, 0);
            throw;
        }
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var ssl = await _server.AcceptAsync(ct);
                _activeSessions.Track(HandleAsync(ssl, ct));
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
            DesktopIncomingTransferPreview? filePreview = null;
            DesktopIncomingTextPreview? textPreview = null;
            try
            {
                var request = await FrameProtocol.ReadJsonAsync<ProtocolRequest>(connection, ct);
                var validationTime = DateTimeOffset.UtcNow;
                try
                {
                    ProtocolRequestValidator.Validate(request, validationTime);
                }
                catch (Exception ex) when (ex is InvalidDataException or NotSupportedException or ArgumentException or OverflowException)
                {
                    await RejectAsync(connection, "Unsupported or invalid transfer request.", ct);
                    return;
                }

                if (connection.RemoteCertificate is null)
                {
                    await RejectAsync(connection, "Sender certificate is required.", ct);
                    return;
                }

                using var senderCertificate = LoadPublicCertificate(connection.RemoteCertificate.GetRawCertData());
                var senderFingerprint = Fingerprint.FromCertificate(senderCertificate);

                if (request.Type == "pair-request")
                {
                    if (!_pairingAttemptLimiter.TryAcquire(senderFingerprint, validationTime))
                    {
                        await RejectAsync(connection, "Too many pairing attempts. Try again shortly.", ct);
                        return;
                    }
                    await HandlePairingRequestAsync(connection, request, senderFingerprint, ct);
                    return;
                }

                try
                {
                    ProtocolSessionAuthorizer.ValidateAndAuthorize(request, validationTime, _consumePairingNonce);
                }
                catch (UnauthorizedAccessException)
                {
                    await RejectAsync(connection, "Pairing authorization failed.", ct);
                    return;
                }

                FileManifestEntry? safeEntry = null;
                IReadOnlyList<FileManifestEntry>? safeFiles = null;
                DateTimeOffset? textExpiry = null;
                if (request.Type == "file")
                {
                    safeEntry = ValidateAndSanitizeEntry(request.Entry!);
                }
                else if (request.Type == "batch")
                {
                    if (_approveBatch is null)
                    {
                        await RejectAsync(connection, "Batch receiving is unavailable.", ct);
                        return;
                    }
                    try
                    {
                        safeFiles = ValidateAndSanitizeBatch(request.Files, request.TotalBytes);
                    }
                    catch (Exception ex) when (ex is InvalidDataException or ArgumentException or IOException or OverflowException)
                    {
                        await RejectAsync(connection, "Unsafe batch metadata.", ct);
                        return;
                    }
                }
                else
                {
                    if (_approveText is null)
                    {
                        await RejectAsync(connection, "Text receiving is unavailable.", ct);
                        return;
                    }
                    textExpiry = DateTimeOffset.FromUnixTimeSeconds(request.ExpiresUnixSeconds!.Value);
                }

                if (request.Type == "text")
                {
                    textPreview = await HandleTextAsync(connection, request, senderFingerprint, textExpiry!.Value, ct);
                    return;
                }

                if (request.Type == "batch")
                {
                    await HandleBatchAsync(connection, request, senderFingerprint, safeFiles!, ct);
                    return;
                }

                filePreview = new DesktopIncomingTransferPreview(
                    request.SenderDeviceId!,
                    request.SenderDeviceName!,
                    senderFingerprint,
                    safeEntry!,
                    FileRiskClassifier.Classify(safeEntry!.RelativePath));

                if (!await _approveTransfer(filePreview, ct))
                {
                    await RejectAsync(connection, "Receiver declined the transfer.", ct);
                    await RecordAsync(filePreview, "rejected", false, ct);
                    return;
                }

                var requestedFinal = PathGuard.ResolveUnderRoot(_receiveRoot, safeEntry.RelativePath);
                using var destination = _destinationReservations.Reserve(requestedFinal);
                var final = destination.Path;
                var effectiveRelativePath = Path.GetRelativePath(_receiveRoot, final);
                var effectiveEntry = safeEntry with { RelativePath = effectiveRelativePath };
                var partial = final + ".swiftdrop.part";
                var offset = File.Exists(partial) ? Math.Min(new FileInfo(partial).Length, effectiveEntry.Length) : 0;
                StorageCapacityGuard.EnsureCapacity(final, effectiveEntry.Length - offset);
                await FrameProtocol.WriteJsonAsync(connection, new TransferAcknowledgement(true, offset), ct);
                var transferStopwatch = Stopwatch.StartNew();
                await new TransferEngine().ReceiveFileAsync(connection, _receiveRoot, effectiveEntry, offset, null, ct);
                transferStopwatch.Stop();
                await FrameProtocol.WriteJsonAsync(connection, new TransferAcknowledgement(true, effectiveEntry.Length), ct);
                await RecordAsync(
                    filePreview with { Entry = effectiveEntry },
                    "completed",
                    true,
                    ct,
                    transferStopwatch.Elapsed,
                    effectiveEntry.Length - offset);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                if (filePreview is not null) await RecordBestEffortAsync(filePreview, "cancelled", false);
                if (textPreview is not null) await RecordTextBestEffortAsync(textPreview, "cancelled");
            }
            catch
            {
                try
                {
                    await FrameProtocol.WriteJsonAsync(
                        connection,
                        new TransferAcknowledgement(false, 0, "Transfer failed safely."),
                        CancellationToken.None);
                }
                catch
                {
                }
                if (filePreview is not null) await RecordBestEffortAsync(filePreview, "failed", false);
                if (textPreview is not null) await RecordTextBestEffortAsync(textPreview, "failed");
            }
        }
    }

    private async Task HandleBatchAsync(
        Stream connection,
        ProtocolRequest request,
        string senderFingerprint,
        IReadOnlyList<FileManifestEntry> safeFiles,
        CancellationToken ct)
    {
        var transferId = IncomingRequestPolicy.ValidateTransferId(request.TransferId);
        var preview = new DesktopIncomingBatchPreview(
            request.SenderDeviceId!,
            request.SenderDeviceName!,
            senderFingerprint,
            transferId,
            safeFiles);
        var decision = await _approveBatch!(preview, ct);
        if (!decision.Accepted)
        {
            await FrameProtocol.WriteJsonAsync(
                connection,
                new BatchTransferResponse(false, Array.Empty<BatchItemPlan>(), "Receiver declined the batch."),
                ct);
            foreach (var file in safeFiles)
            {
                await RecordAsync(
                    new DesktopIncomingTransferPreview(
                        request.SenderDeviceId!,
                        request.SenderDeviceName!,
                        senderFingerprint,
                        file,
                        FileRiskClassifier.Classify(file.RelativePath)),
                    "rejected",
                    false,
                    ct);
            }
            return;
        }

        var knownPaths = safeFiles.Select(x => x.RelativePath).ToHashSet(StringComparer.Ordinal);
        if (decision.AcceptedRelativePaths.Any(path => !knownPaths.Contains(path)))
            throw new InvalidDataException("Receiver selection contained an unknown batch path.");

        var reservations = new List<DestinationReservationSet.DestinationReservation>();
        try
        {
            var receiveItems = new List<BatchReceiveItem>();
            var plans = new List<BatchItemPlan>(safeFiles.Count);
            foreach (var file in safeFiles)
            {
                if (!decision.AcceptedRelativePaths.Contains(file.RelativePath))
                {
                    plans.Add(new BatchItemPlan(file.RelativePath, 0, false, "Not selected by receiver."));
                    await RecordAsync(
                        new DesktopIncomingTransferPreview(
                            request.SenderDeviceId!,
                            request.SenderDeviceName!,
                            senderFingerprint,
                            file,
                            FileRiskClassifier.Classify(file.RelativePath)),
                        "not-selected",
                        false,
                        ct);
                    continue;
                }

                var completed = await _batchResumeState.TryGetVerifiedAsync(transferId, file, ct);
                if (completed is not null)
                {
                    var effectiveCompletedEntry = file with { RelativePath = completed.DestinationRelativePath };
                    plans.Add(new BatchItemPlan(file.RelativePath, file.Length, true));
                    receiveItems.Add(new BatchReceiveItem(file.RelativePath, effectiveCompletedEntry, file.Length, true));
                    continue;
                }

                var requestedFinal = PathGuard.ResolveUnderRoot(_receiveRoot, file.RelativePath);
                var destination = _destinationReservations.Reserve(requestedFinal);
                reservations.Add(destination);
                var final = destination.Path;
                var effectiveRelativePath = Path.GetRelativePath(_receiveRoot, final);
                var effectiveEntry = file with { RelativePath = effectiveRelativePath };
                var partial = final + ".swiftdrop.part";
                var offset = File.Exists(partial) ? Math.Min(new FileInfo(partial).Length, effectiveEntry.Length) : 0;
                plans.Add(new BatchItemPlan(file.RelativePath, offset, true));
                receiveItems.Add(new BatchReceiveItem(file.RelativePath, effectiveEntry, offset, false));
            }

            if (receiveItems.Count == 0)
            {
                await FrameProtocol.WriteJsonAsync(
                    connection,
                    new BatchTransferResponse(false, plans, "No files were selected."),
                    ct);
                return;
            }

            var aggregateRemainingBytes = receiveItems.Aggregate(
                0L,
                static (total, item) => checked(total + item.EffectiveEntry.Length - item.ResumeOffset));
            StorageCapacityGuard.EnsureCapacity(_receiveRoot, aggregateRemainingBytes);

            await FrameProtocol.WriteJsonAsync(connection, new BatchTransferResponse(true, plans), ct);
            foreach (var item in receiveItems)
            {
                var start = await FrameProtocol.ReadJsonAsync<BatchItemStart>(connection, ct);
                IncomingRequestPolicy.ValidateBatchItemStart(item.SourceRelativePath, start.RelativePath);

                if (item.AlreadyCompleted)
                {
                    var sourceEntry = item.EffectiveEntry with { RelativePath = item.SourceRelativePath };
                    var reverified = await _batchResumeState.TryGetVerifiedAsync(transferId, sourceEntry, ct);
                    if (reverified is null ||
                        !string.Equals(
                            reverified.DestinationRelativePath,
                            item.EffectiveEntry.RelativePath,
                            PathComparisonPolicy.Comparison))
                    {
                        throw new IOException("Completed batch item changed after resume planning.");
                    }

                    await FrameProtocol.WriteJsonAsync(
                        connection,
                        new TransferAcknowledgement(true, item.EffectiveEntry.Length),
                        ct);
                    continue;
                }

                var itemPreview = new DesktopIncomingTransferPreview(
                    request.SenderDeviceId!,
                    request.SenderDeviceName!,
                    senderFingerprint,
                    item.EffectiveEntry,
                    FileRiskClassifier.Classify(item.EffectiveEntry.RelativePath));
                try
                {
                    var itemStopwatch = Stopwatch.StartNew();
                    await new TransferEngine().ReceiveFileAsync(
                        connection,
                        _receiveRoot,
                        item.EffectiveEntry,
                        item.ResumeOffset,
                        null,
                        ct);
                    itemStopwatch.Stop();
                    await _batchResumeState.RecordCompletedAsync(
                        transferId,
                        item.SourceRelativePath,
                        item.EffectiveEntry,
                        ct);
                    await FrameProtocol.WriteJsonAsync(
                        connection,
                        new TransferAcknowledgement(true, item.EffectiveEntry.Length),
                        ct);
                    await RecordAsync(
                        itemPreview,
                        "completed",
                        true,
                        ct,
                        itemStopwatch.Elapsed,
                        item.EffectiveEntry.Length - item.ResumeOffset);
                }
                catch
                {
                    await RecordBestEffortAsync(itemPreview, "failed", false);
                    throw;
                }
            }

            await FrameProtocol.WriteJsonAsync(connection, new TransferAcknowledgement(true, preview.TotalBytes), ct);
        }
        finally
        {
            foreach (var reservation in reservations)
                reservation.Dispose();
        }
    }

    private static FileManifestEntry ValidateAndSanitizeEntry(FileManifestEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var sanitizedPath = FileNameSanitizer.SanitizeRelativePath(entry.RelativePath);
        return ManifestValidator.ValidateEntry(entry with { RelativePath = sanitizedPath });
    }

    private IReadOnlyList<FileManifestEntry> ValidateAndSanitizeBatch(
        IReadOnlyList<FileManifestEntry>? files,
        long? declaredTotal)
    {
        if (files is null)
            throw new InvalidDataException("Batch metadata is required.");

        var safe = files.Select(ValidateAndSanitizeEntry).ToArray();
        var validated = BatchManifestValidator.Validate(safe, declaredTotal);
        foreach (var file in validated)
            _ = PathGuard.ResolveUnderRoot(_receiveRoot, file.RelativePath);
        return validated;
    }

    private async Task HandlePairingRequestAsync(
        Stream connection,
        ProtocolRequest request,
        string senderFingerprint,
        CancellationToken ct)
    {
        if (_approvePairing is null || _createPairingLink is null)
        {
            await FrameProtocol.WriteJsonAsync(connection, new PairingResponse(false, "Nearby pairing is unavailable.", null), ct);
            return;
        }

        if (!string.IsNullOrWhiteSpace(request.PairingCode))
        {
            if (_consumePairingCode is null || !_consumePairingCode(request.PairingCode))
            {
                await FrameProtocol.WriteJsonAsync(connection, new PairingResponse(false, "Pairing code is invalid or expired.", null), ct);
                return;
            }
        }

        var preview = new DesktopIncomingPairingRequest(
            request.SenderDeviceId!,
            request.SenderDeviceName!,
            senderFingerprint);
        if (!await _approvePairing(preview, ct))
        {
            await FrameProtocol.WriteJsonAsync(connection, new PairingResponse(false, "Receiver declined pairing.", null), ct);
            return;
        }

        var link = _createPairingLink();
        await FrameProtocol.WriteJsonAsync(connection, new PairingResponse(true, null, link), ct);
    }

    private async Task<DesktopIncomingTextPreview> HandleTextAsync(
        Stream connection,
        ProtocolRequest request,
        string senderFingerprint,
        DateTimeOffset expiresUtc,
        CancellationToken ct)
    {
        var preview = new DesktopIncomingTextPreview(
            request.SenderDeviceId!,
            request.SenderDeviceName!,
            senderFingerprint,
            request.Text!,
            expiresUtc);
        var decision = await _approveText!(preview, ct);
        if (decision == DesktopIncomingTextDecision.Reject)
        {
            await RejectAsync(connection, "Receiver declined the text snippet.", ct);
            await RecordTextAsync(preview, "rejected", ct);
            return preview;
        }

        await FrameProtocol.WriteJsonAsync(connection, new TransferAcknowledgement(true, 0), ct);
        await RecordTextAsync(
            preview,
            decision == DesktopIncomingTextDecision.AcceptAndCopy ? "copied" : "accepted",
            ct);
        return preview;
    }

    private static Task RejectAsync(Stream connection, string message, CancellationToken ct)
        => FrameProtocol.WriteJsonAsync(connection, new TransferAcknowledgement(false, 0, message), ct);

    private Task RecordAsync(
        DesktopIncomingTransferPreview preview,
        string status,
        bool verified,
        CancellationToken ct,
        TimeSpan? duration = null,
        long? measuredBytes = null)
        => _recordTransfer?.Invoke(preview, status, verified, duration, measuredBytes, ct) ?? Task.CompletedTask;

    private Task RecordTextAsync(DesktopIncomingTextPreview preview, string status, CancellationToken ct)
        => _recordText?.Invoke(preview, status, ct) ?? Task.CompletedTask;

    private async Task RecordBestEffortAsync(DesktopIncomingTransferPreview preview, string status, bool verified)
    {
        try { await RecordAsync(preview, status, verified, CancellationToken.None); }
        catch
        {
        }
    }

    private async Task RecordTextBestEffortAsync(DesktopIncomingTextPreview preview, string status)
    {
        try { await RecordTextAsync(preview, status, CancellationToken.None); }
        catch
        {
        }
    }

    private static X509Certificate2 CopyCertificateWithPrivateKey(X509Certificate2 certificate)
    {
        var pfx = certificate.Export(X509ContentType.Pfx);
        try
        {
            return X509CertificateLoader.LoadPkcs12(pfx, null);
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(pfx);
        }
    }

    private static X509Certificate2 LoadPublicCertificate(byte[] rawData)
        => X509CertificateLoader.LoadCertificate(rawData);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

        _cts.Cancel();
        await _server.DisposeAsync();
        if (_loop is not null)
        {
            try { await _loop; }
            catch (OperationCanceledException)
            {
            }
        }

        await _activeSessions.DrainAsync();
        _cts.Dispose();
    }

    private sealed record BatchReceiveItem(
        string SourceRelativePath,
        FileManifestEntry EffectiveEntry,
        long ResumeOffset,
        bool AlreadyCompleted);
}
