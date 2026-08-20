using SwiftDrop.Core.Models;
using SwiftDrop.Core.Networking;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Desktop.Services;

public sealed class DesktopTransferClient
{
    private readonly DesktopIdentityService _identity;

    public DesktopTransferClient(DesktopIdentityService identity)
    {
        _identity = identity;
    }

    public async Task<DesktopFileSendResult> SendFileAsync(
        PairingPayload remote,
        string path,
        IProgress<double>? progress,
        CancellationToken ct)
    {
        remote = await PrepareRemoteAsync(remote, ct);
        var info = TransferSourceSafety.GetRegularFile(path);
        if (info.Length > ProtocolConstants.MaxSingleFileBytes)
            throw new InvalidDataException("File exceeds SwiftDrop safety limit.");

        var entry = ManifestValidator.ValidateEntry(new FileManifestEntry(
            FileNameSanitizer.SanitizeSegment(info.Name),
            info.Length,
            await Hashing.Sha256FileAsync(info.FullName, ct),
            info.LastWriteTimeUtc));
        var request = ProtocolRequestFactory.CreateFile(
            remote.Nonce,
            _identity.DeviceId,
            _identity.DeviceName,
            entry);

        var client = new TlsPeerClient();
        await using var ssl = await client.ConnectAsync(
            remote.Host,
            remote.Port,
            remote.CertificateFingerprint,
            _identity.Certificate,
            ct);
        await FrameProtocol.WriteJsonAsync(ssl, request, ct);
        var response = await FrameProtocol.ReadJsonAsync<TransferAcknowledgement>(ssl, ct);
        var resumeOffset = TransferResponsePolicy.ValidateResumeOffset(
            response.Accepted,
            response.ResumeOffset,
            entry.Length,
            response.Message);

        progress?.Report(entry.Length == 0 ? 1 : (double)resumeOffset / entry.Length);
        var bytesProgress = new Progress<long>(sent => progress?.Report(entry.Length == 0 ? 1 : (double)sent / entry.Length));
        await new TransferEngine().SendFileAsync(
            ssl,
            info.FullName,
            resumeOffset,
            entry.Length,
            bytesProgress,
            ct);
        var completed = await FrameProtocol.ReadJsonAsync<TransferAcknowledgement>(ssl, ct);
        TransferResponsePolicy.ValidateCompletion(
            completed.Accepted,
            completed.ResumeOffset,
            entry.Length,
            completed.Message);
        progress?.Report(1);
        return new DesktopFileSendResult(entry.Length, entry.Length - resumeOffset);
    }

    public async Task<DesktopBatchSendResult> SendBatchAsync(
        PairingPayload remote,
        IEnumerable<string> paths,
        string transferId,
        IProgress<DesktopBatchProgress>? progress,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(paths);
        remote = await PrepareRemoteAsync(remote, ct);
        transferId = IncomingRequestPolicy.ValidateTransferId(transferId);
        var batch = await BatchTransferSourceBuilder.BuildAsync(paths, transferId, ct);
        var entries = batch.Items.Select(x => x.Entry).ToArray();
        var request = ProtocolRequestFactory.CreateBatch(
            remote.Nonce,
            _identity.DeviceId,
            _identity.DeviceName,
            batch.TransferId,
            entries,
            batch.TotalBytes);

        var client = new TlsPeerClient();
        await using var ssl = await client.ConnectAsync(
            remote.Host,
            remote.Port,
            remote.CertificateFingerprint,
            _identity.Certificate,
            ct);
        await FrameProtocol.WriteJsonAsync(ssl, request, ct);
        var response = await FrameProtocol.ReadJsonAsync<BatchTransferResponse>(ssl, ct);
        var plans = BatchTransferPlanValidator.Validate(entries, response);
        if (!response.Accepted)
            throw new IOException(response.Message ?? "Receiver rejected the batch transfer.");

        var sourceByPath = batch.Items.ToDictionary(x => x.Entry.RelativePath, StringComparer.Ordinal);
        var acceptedPlans = plans
            .Where(x => x.Value.Accepted)
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        var acceptedTotal = acceptedPlans.Values.Sum(plan => sourceByPath[plan.RelativePath].Entry.Length);
        long completedBefore = 0;
        var completedItems = 0;
        var completedSources = new List<FileTransferSource>(acceptedPlans.Count);

        foreach (var source in batch.Items)
        {
            ct.ThrowIfCancellationRequested();
            if (!acceptedPlans.TryGetValue(source.Entry.RelativePath, out var plan)) continue;

            var currentBase = completedBefore;
            progress?.Report(new DesktopBatchProgress(
                completedItems,
                acceptedPlans.Count,
                currentBase + plan.ResumeOffset,
                acceptedTotal,
                source.Entry.RelativePath));
            await FrameProtocol.WriteJsonAsync(ssl, new BatchItemStart(source.Entry.RelativePath), ct);
            var itemProgress = new Progress<long>(sent => progress?.Report(new DesktopBatchProgress(
                completedItems,
                acceptedPlans.Count,
                currentBase + sent,
                acceptedTotal,
                source.Entry.RelativePath)));
            await new TransferEngine().SendFileAsync(
                ssl,
                source.LocalPath,
                plan.ResumeOffset,
                source.Entry.Length,
                itemProgress,
                ct);
            var itemResponse = await FrameProtocol.ReadJsonAsync<TransferAcknowledgement>(ssl, ct);
            TransferResponsePolicy.ValidateCompletion(
                itemResponse.Accepted,
                itemResponse.ResumeOffset,
                source.Entry.Length,
                itemResponse.Message);

            completedBefore += source.Entry.Length;
            completedItems++;
            completedSources.Add(source);
            progress?.Report(new DesktopBatchProgress(
                completedItems,
                acceptedPlans.Count,
                completedBefore,
                acceptedTotal,
                source.Entry.RelativePath));
        }

        var final = await FrameProtocol.ReadJsonAsync<TransferAcknowledgement>(ssl, ct);
        TransferResponsePolicy.ValidateCompletion(final.Accepted, final.ResumeOffset, batch.TotalBytes, final.Message);
        var skipped = batch.Items.Where(x => !acceptedPlans.ContainsKey(x.Entry.RelativePath)).ToArray();
        return new DesktopBatchSendResult(batch.TransferId, completedSources, skipped);
    }

    public async Task SendTextAsync(PairingPayload remote, string text, CancellationToken ct)
    {
        remote = await PrepareRemoteAsync(remote, ct);
        var now = DateTimeOffset.UtcNow;
        var request = ProtocolRequestFactory.CreateText(
            remote.Nonce,
            _identity.DeviceId,
            _identity.DeviceName,
            text,
            now.Add(ProtocolConstants.TextSnippetLifetime),
            now);

        var client = new TlsPeerClient();
        await using var ssl = await client.ConnectAsync(
            remote.Host,
            remote.Port,
            remote.CertificateFingerprint,
            _identity.Certificate,
            ct);
        await FrameProtocol.WriteJsonAsync(ssl, request, ct);
        var response = await FrameProtocol.ReadJsonAsync<TransferAcknowledgement>(ssl, ct);
        TransferResponsePolicy.ValidateTextAcknowledgement(response.Accepted, response.ResumeOffset, response.Message);
    }

    private async Task<PairingPayload> PrepareRemoteAsync(PairingPayload remote, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(remote);
        ct.ThrowIfCancellationRequested();
        await _identity.InitializeAsync();
        return PairingCodec.Validate(remote, DateTimeOffset.UtcNow);
    }
}

public sealed record DesktopFileSendResult(long LogicalBytes, long TransferredBytes);

public sealed record DesktopBatchProgress(
    int CompletedItems,
    int TotalItems,
    long CompletedBytes,
    long TotalBytes,
    string CurrentFile)
{
    public double Fraction => TotalBytes <= 0 ? 1 : Math.Clamp((double)CompletedBytes / TotalBytes, 0, 1);
}

public sealed record DesktopBatchSendResult(
    string TransferId,
    IReadOnlyList<FileTransferSource> Completed,
    IReadOnlyList<FileTransferSource> Skipped);
