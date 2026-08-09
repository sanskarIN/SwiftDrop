using SwiftDrop.Core.Models;
using SwiftDrop.Core.Networking;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.App.Services;

public sealed class TransferCoordinator
{
    private readonly DeviceIdentityService _identity;

    public TransferCoordinator(DeviceIdentityService identity)
    {
        _identity = identity;
    }

    public async Task SendAsync(PairingPayload remote, string path, IProgress<double>? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(remote);
        if (!File.Exists(path)) throw new FileNotFoundException("Selected file cannot be opened.", path);
        var info = new FileInfo(path);
        if (info.Length > ProtocolConstants.MaxSingleFileBytes) throw new InvalidDataException("File exceeds SwiftDrop safety limit.");
        var entry = new FileManifestEntry(Path.GetFileName(path), info.Length, await Hashing.Sha256FileAsync(path, ct), info.LastWriteTimeUtc);
        var client = new TlsPeerClient();
        await using var ssl = await client.ConnectAsync(remote.Host, remote.Port, remote.CertificateFingerprint, _identity.Certificate, ct);
        await FrameProtocol.WriteJsonAsync(ssl, new
        {
            type = "file",
            protocolVersion = ProtocolConstants.CurrentVersion,
            pairingNonce = remote.Nonce,
            senderDeviceId = _identity.DeviceId,
            senderDeviceName = _identity.DeviceName,
            entry
        }, ct);
        var response = await FrameProtocol.ReadJsonAsync<TransferResponse>(ssl, ct);
        if (!response.Accepted) throw new IOException(response.Message ?? "Receiver rejected the transfer.");
        var bytesProgress = new Progress<long>(sent => progress?.Report(info.Length == 0 ? 1 : (double)sent / info.Length));
        await new TransferEngine().SendFileAsync(ssl, path, response.ResumeOffset, bytesProgress, ct);
        var completed = await FrameProtocol.ReadJsonAsync<TransferResponse>(ssl, ct);
        if (!completed.Accepted) throw new IOException(completed.Message ?? "Receiver reported failure.");
    }

    public async Task<BatchSendResult> SendBatchAsync(
        PairingPayload remote,
        IEnumerable<string> paths,
        IProgress<BatchProgress>? progress,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(remote);
        var batch = await BatchTransferSourceBuilder.BuildAsync(paths, ct);
        var client = new TlsPeerClient();
        await using var ssl = await client.ConnectAsync(
            remote.Host,
            remote.Port,
            remote.CertificateFingerprint,
            _identity.Certificate,
            ct);

        await FrameProtocol.WriteJsonAsync(ssl, new
        {
            type = "batch",
            protocolVersion = ProtocolConstants.CurrentVersion,
            pairingNonce = remote.Nonce,
            senderDeviceId = _identity.DeviceId,
            senderDeviceName = _identity.DeviceName,
            transferId = batch.TransferId,
            files = batch.Items.Select(x => x.Entry).ToArray(),
            totalBytes = batch.TotalBytes
        }, ct);

        var response = await FrameProtocol.ReadJsonAsync<BatchTransferResponse>(ssl, ct);
        if (!response.Accepted)
            throw new IOException(response.Message ?? "Receiver rejected the batch transfer.");

        var sourceByPath = batch.Items.ToDictionary(x => x.Entry.RelativePath, StringComparer.Ordinal);
        var acceptedPlans = new Dictionary<string, BatchItemPlan>(StringComparer.Ordinal);
        foreach (var plan in response.Items)
        {
            if (!sourceByPath.TryGetValue(plan.RelativePath, out var source))
                throw new InvalidDataException("Receiver returned an unknown batch item.");
            if (plan.ResumeOffset < 0 || plan.ResumeOffset > source.Entry.Length)
                throw new InvalidDataException("Receiver returned an invalid resume offset.");
            if (plan.Accepted && !acceptedPlans.TryAdd(plan.RelativePath, plan))
                throw new InvalidDataException("Receiver returned a duplicate batch item.");
        }

        if (acceptedPlans.Count == 0)
            throw new IOException("Receiver did not accept any files in the batch.");

        var acceptedTotal = acceptedPlans.Values.Sum(plan => sourceByPath[plan.RelativePath].Entry.Length);
        long completedBefore = 0;
        var completedItems = 0;
        var completedSources = new List<FileTransferSource>(acceptedPlans.Count);
        foreach (var source in batch.Items)
        {
            ct.ThrowIfCancellationRequested();
            if (!acceptedPlans.TryGetValue(source.Entry.RelativePath, out var plan)) continue;

            await FrameProtocol.WriteJsonAsync(ssl, new BatchItemStart(source.Entry.RelativePath), ct);
            var currentBase = completedBefore;
            var itemProgress = new Progress<long>(sent =>
            {
                var totalCompleted = currentBase + sent;
                progress?.Report(new BatchProgress(
                    completedItems,
                    acceptedPlans.Count,
                    totalCompleted,
                    acceptedTotal,
                    source.Entry.RelativePath));
            });
            await new TransferEngine().SendFileAsync(ssl, source.LocalPath, plan.ResumeOffset, itemProgress, ct);
            var itemResponse = await FrameProtocol.ReadJsonAsync<TransferResponse>(ssl, ct);
            if (!itemResponse.Accepted)
                throw new IOException(itemResponse.Message ?? $"Receiver failed while saving {source.Entry.RelativePath}.");

            completedBefore += source.Entry.Length;
            completedItems++;
            completedSources.Add(source);
            progress?.Report(new BatchProgress(
                completedItems,
                acceptedPlans.Count,
                completedBefore,
                acceptedTotal,
                source.Entry.RelativePath));
        }

        var final = await FrameProtocol.ReadJsonAsync<TransferResponse>(ssl, ct);
        if (!final.Accepted) throw new IOException(final.Message ?? "Receiver reported batch failure.");

        var skipped = batch.Items
            .Where(x => !acceptedPlans.ContainsKey(x.Entry.RelativePath))
            .ToArray();
        return new BatchSendResult(completedSources, skipped);
    }

    public async Task SendTextAsync(PairingPayload remote, string text, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(remote);
        var now = DateTimeOffset.UtcNow;
        var expires = now.Add(ProtocolConstants.TextSnippetLifetime);
        TextSnippetValidator.Validate(text, expires, now);

        var client = new TlsPeerClient();
        await using var ssl = await client.ConnectAsync(remote.Host, remote.Port, remote.CertificateFingerprint, _identity.Certificate, ct);
        await FrameProtocol.WriteJsonAsync(ssl, new
        {
            type = "text",
            protocolVersion = ProtocolConstants.CurrentVersion,
            pairingNonce = remote.Nonce,
            senderDeviceId = _identity.DeviceId,
            senderDeviceName = _identity.DeviceName,
            text,
            expiresUnixSeconds = expires.ToUnixTimeSeconds()
        }, ct);
        var response = await FrameProtocol.ReadJsonAsync<TransferResponse>(ssl, ct);
        if (!response.Accepted) throw new IOException(response.Message ?? "Receiver rejected the text snippet.");
    }

    private sealed record TransferResponse(bool Accepted, long ResumeOffset, string? Message);
    private sealed record BatchItemStart(string RelativePath);
}

public sealed record BatchProgress(
    int CompletedItems,
    int TotalItems,
    long CompletedBytes,
    long TotalBytes,
    string CurrentFile)
{
    public double Fraction => TotalBytes <= 0 ? 1 : Math.Clamp((double)CompletedBytes / TotalBytes, 0, 1);
}

public sealed record BatchSendResult(
    IReadOnlyList<FileTransferSource> Completed,
    IReadOnlyList<FileTransferSource> Skipped);
