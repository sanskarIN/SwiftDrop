using System.Collections.Concurrent;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
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
    private readonly Func<IncomingBatchPreview, CancellationToken, Task<IncomingBatchDecision>>? _approveBatch;
    private readonly Func<IncomingTextPreview, CancellationToken, Task<IncomingTextDecision>>? _approveText;
    private readonly Func<IncomingTextPreview, string, CancellationToken, Task>? _recordText;
    private readonly Func<IncomingPairingRequest, CancellationToken, Task<bool>>? _approvePairing;
    private readonly Func<string>? _createPairingLink;
    private readonly Func<string?, bool>? _consumePairingCode;
    private readonly AttemptRateLimiter _pairingAttemptLimiter = new(8, TimeSpan.FromMinutes(1));
    private readonly DestinationReservationSet _destinationReservations = new();
    private readonly ConcurrentDictionary<long, Task> _activeHandlers = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;
    private long _nextHandlerId;
    private int _started;
    private int _disposed;

    public ReceiveServerService(
        X509Certificate2 certificate,
        string receiveRoot,
        Func<string, bool> consumePairingNonce,
        Func<IncomingTransferPreview, CancellationToken, Task<bool>> approveTransfer,
        Func<IncomingTransferPreview, string, bool, CancellationToken, Task>? recordTransfer = null,
        Func<IncomingTextPreview, CancellationToken, Task<IncomingTextDecision>>? approveText = null,
        Func<IncomingTextPreview, string, CancellationToken, Task>? recordText = null,
        Func<IncomingPairingRequest, CancellationToken, Task<bool>>? approvePairing = null,
        Func<string>? createPairingLink = null,
        Func<string?, bool>? consumePairingCode = null,
        Func<IncomingBatchPreview, CancellationToken, Task<IncomingBatchDecision>>? approveBatch = null)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        ArgumentException.ThrowIfNullOrWhiteSpace(receiveRoot);
        _receiveRoot = receiveRoot;
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
                var id = Interlocked.Increment(ref _nextHandlerId);
                var handler = HandleAsync(ssl, ct);
                _activeHandlers[id] = handler;
                _ = ObserveHandlerAsync(id, handler);
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

    private async Task ObserveHandlerAsync(long id, Task handler)
    {
        try
        {
            await handler;
        }
        catch
        {
        }
        finally
        {
            _activeHandlers.TryRemove(id, out _);
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
                var request = await FrameProtocol.ReadJsonAsync<ProtocolRequest>(connection, ct);
                try
                {
                    IncomingRequestPolicy.ValidateEnvelope(request.ProtocolVersion, request.Type);
                    IncomingRequestPolicy.ValidateSenderIdentity(request.SenderDeviceId, request.SenderDeviceName);
                }
                catch (Exception ex) when (ex is InvalidDataException or NotSupportedException)
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
                    if (!_pairingAttemptLimiter.TryAcquire(senderFingerprint, DateTimeOffset.UtcNow))
                    {
                        await RejectAsync(connection, "Too many pairing attempts. Try again shortly.", ct);
                        return;
                    }
                    await HandlePairingRequestAsync(connection, request, senderFingerprint, ct);
                    return;
                }

                FileManifestEntry? safeEntry = null;
                IReadOnlyList<FileManifestEntry>? safeFiles = null;
                DateTimeOffset? textExpiry = null;
                if (request.Type == "file")
                {
                    if (request.Entry is null)
                    {
                        await RejectAsync(connection, "File metadata is required.", ct);
                        return;
                    }
                    safeEntry = ValidateAndSanitizeEntry(request.Entry);
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
                    try
                    {
                        textExpiry = DateTimeOffset.FromUnixTimeSeconds(request.ExpiresUnixSeconds ?? 0);
                        TextSnippetValidator.Validate(request.Text, textExpiry.Value, DateTimeOffset.UtcNow);
                    }
                    catch (Exception ex) when (ex is InvalidDataException or ArgumentOutOfRangeException)
                    {
                        await RejectAsync(connection, "Text snippet is invalid or expired.", ct);
                        return;
                    }
                }

                if (string.IsNullOrWhiteSpace(request.PairingNonce) || !_consumePairingNonce(request.PairingNonce))
                {
                    await RejectAsync(connection, "Pairing authorization failed.", ct);
                    return;
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

                filePreview = new IncomingTransferPreview(
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
                await new TransferEngine().ReceiveFileAsync(connection, _receiveRoot, effectiveEntry, offset, null, ct);
                await FrameProtocol.WriteJsonAsync(connection, new TransferAcknowledgement(true, effectiveEntry.Length), ct);
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
                    await FrameProtocol.WriteJsonAsync(connection, new TransferAcknowledgement(false, 0, "Transfer failed safely."), CancellationToken.None);
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
        string transferId;
        try
        {
            transferId = IncomingRequestPolicy.ValidateTransferId(request.TransferId);
        }
        catch (InvalidDataException)
        {
            await FrameProtocol.WriteJsonAsync(
                connection,
                new BatchTransferResponse(false, Array.Empty<BatchItemPlan>(), "Invalid transfer identifier."),
                ct);
            return;
        }

        var preview = new IncomingBatchPreview(
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
                    new IncomingTransferPreview(request.SenderDeviceId!, request.SenderDeviceName!, senderFingerprint, file, FileRiskClassifier.Classify(file.RelativePath)),
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
                        new IncomingTransferPreview(request.SenderDeviceId!, request.SenderDeviceName!, senderFingerprint, file, FileRiskClassifier.Classify(file.RelativePath)),
                        "not-selected",
                        false,
                        ct);
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
                receiveItems.Add(new BatchReceiveItem(file.RelativePath, effectiveEntry, offset));
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

                var itemPreview = new IncomingTransferPreview(
                    request.SenderDeviceId!,
                    request.SenderDeviceName!,
                    senderFingerprint,
                    item.EffectiveEntry,
                    FileRiskClassifier.Classify(item.EffectiveEntry.RelativePath));
                try
                {
                    await new TransferEngine().ReceiveFileAsync(
                        connection,
                        _receiveRoot,
                        item.EffectiveEntry,
                        item.ResumeOffset,
                        null,
                        ct);
                    await FrameProtocol.WriteJsonAsync(connection, new TransferAcknowledgement(true, item.EffectiveEntry.Length), ct);
                    await RecordAsync(itemPreview, "completed", true, ct);
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

        var preview = new IncomingPairingRequest(request.SenderDeviceId!, request.SenderDeviceName!, senderFingerprint);
        if (!await _approvePairing(preview, ct))
        {
            await FrameProtocol.WriteJsonAsync(connection, new PairingResponse(false, "Receiver declined pairing.", null), ct);
            return;
        }

        var link = _createPairingLink();
        await FrameProtocol.WriteJsonAsync(connection, new PairingResponse(true, null, link), ct);
    }

    private async Task<IncomingTextPreview> HandleTextAsync(
        Stream connection,
        ProtocolRequest request,
        string senderFingerprint,
        DateTimeOffset expiresUtc,
        CancellationToken ct)
    {
        var preview = new IncomingTextPreview(
            request.SenderDeviceId!,
            request.SenderDeviceName!,
            senderFingerprint,
            request.Text!,
            expiresUtc);
        var decision = await _approveText!(preview, ct);
        if (decision == IncomingTextDecision.Reject)
        {
            await RejectAsync(connection, "Receiver declined the text snippet.", ct);
            await RecordTextAsync(preview, "rejected", ct);
            return preview;
        }

        await FrameProtocol.WriteJsonAsync(connection, new TransferAcknowledgement(true, 0), ct);
        await RecordTextAsync(preview, decision == IncomingTextDecision.AcceptAndCopy ? "copied" : "accepted", ct);
        return preview;
    }

    private static Task RejectAsync(Stream connection, string message, CancellationToken ct)
        => FrameProtocol.WriteJsonAsync(connection, new TransferAcknowledgement(false, 0, message), ct);

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
            try { await _loop; } catch (OperationCanceledException) { }
        }

        var active = _activeHandlers.Values.ToArray();
        if (active.Length > 0)
        {
            try { await Task.WhenAll(active); } catch { }
        }

        _activeHandlers.Clear();
        _cts.Dispose();
    }

    private sealed record BatchReceiveItem(string SourceRelativePath, FileManifestEntry EffectiveEntry, long ResumeOffset);
}
