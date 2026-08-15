using SwiftDrop.Core.Models;
using SwiftDrop.Core.Networking;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.App.Services;

public sealed class TransferCoordinator
{
    private readonly DeviceIdentityService _identity;
    private readonly TransferQueueService _queue;

    public TransferCoordinator(DeviceIdentityService identity, TransferQueueService queue)
    {
        _identity = identity;
        _queue = queue;
    }

    public Task SendAsync(PairingPayload remote, string path, IProgress<double>? progress, CancellationToken ct)
        => _queue.ExecuteAsync(
            $"Send {Path.GetFileName(path)}",
            TransferQueueOperationKind.File,
            1,
            (queueProgress, token) => SendCoreAsync(
                remote,
                path,
                new ForwardingProgress<double>(value =>
                {
                    queueProgress.Report(value, value >= 1d ? 1 : 0, 1);
                    progress?.Report(value);
                }),
                token),
            ct);

    public Task<BatchSendResult> SendBatchAsync(
        PairingPayload remote,
        IEnumerable<string> paths,
        string transferId,
        IProgress<BatchProgress>? progress,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(paths);
        transferId = IncomingRequestPolicy.ValidateTransferId(transferId);
        var materialized = paths.ToArray();
        var label = materialized.Length == 1
            ? $"Send {Path.GetFileName(materialized[0])}"
            : $"Send {materialized.Length:N0} files";
        return _queue.ExecuteAsync(
            label,
            TransferQueueOperationKind.Batch,
            materialized.Length,
            (queueProgress, token) => SendBatchCoreAsync(
                remote,
                materialized,
                transferId,
                new ForwardingProgress<BatchProgress>(value =>
                {
                    queueProgress.Report(value.Fraction, value.CompletedItems, value.TotalItems);
                    progress?.Report(value);
                }),
                token),
            ct);
    }

    public Task SendTextAsync(PairingPayload remote, string text, CancellationToken ct)
        => _queue.ExecuteAsync(
            "Send text snippet",
            TransferQueueOperationKind.Text,
            1,
            async (queueProgress, token) =>
            {
                await SendTextCoreAsync(remote, text, token);
                queueProgress.Report(1d, 1, 1);
            },
            ct);

    private async Task SendCoreAsync(PairingPayload remote, string path, IProgress<double>? progress, CancellationToken ct)
    {
        remote = await PrepareRemoteAsync(remote, ct);
        var info = TransferSourceSafety.GetRegularFile(path);
        if (info.Length > ProtocolConstants.MaxSingleFileBytes) throw new InvalidDataException("File exceeds SwiftDrop safety limit.");
        var safeName = FileNameSanitizer.SanitizeSegment(info.Name);
        var entry = ManifestValidator.ValidateEntry(new FileManifestEntry(
            safeName,
            info.Length,
            await Hashing.Sha256FileAsync(info.FullName, ct),
            info.LastWriteTimeUtc));
        var request = ProtocolRequestFactory.CreateFile(
            remote.Nonce,
            _identity.DeviceId,
            _identity.DeviceName,
            entry);

        var client = new TlsPeerClient();
        await using var ssl = await client.ConnectAsync(remote.Host, remote.Port, remote.CertificateFingerprint, _identity.Certificate, ct);
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
    }

    private async Task<BatchSendResult> SendBatchCoreAsync(
        PairingPayload remote,
        IEnumerable<string> paths,
        string transferId,
        IProgress<BatchProgress>? progress,
        CancellationToken ct)
    {
        remote = await PrepareRemoteAsync(remote, ct);
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
        var validatedPlans = BatchTransferPlanValidator.Validate(entries, response);
        if (!response.Accepted)
            throw new IOException(response.Message ?? "Receiver rejected the batch transfer.");

        var sourceByPath = batch.Items.ToDictionary(x => x.Entry.RelativePath, StringComparer.Ordinal);
        var acceptedPlans = validatedPlans
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
            progress?.Report(new BatchProgress(
                completedItems,
                acceptedPlans.Count,
                currentBase + plan.ResumeOffset,
                acceptedTotal,
                source.Entry.RelativePath));

            await FrameProtocol.WriteJsonAsync(ssl, new BatchItemStart(source.Entry.RelativePath), ct);
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
            progress?.Report(new BatchProgress(
                completedItems,
                acceptedPlans.Count,
                completedBefore,
                acceptedTotal,
                source.Entry.RelativePath));
        }

        var final = await FrameProtocol.ReadJsonAsync<TransferAcknowledgement>(ssl, ct);
        TransferResponsePolicy.ValidateCompletion(
            final.Accepted,
            final.ResumeOffset,
            batch.TotalBytes,
            final.Message);

        var skipped = batch.Items
            .Where(x => !acceptedPlans.ContainsKey(x.Entry.RelativePath))
            .ToArray();
        return new BatchSendResult(batch.TransferId, completedSources, skipped);
    }

    private async Task SendTextCoreAsync(PairingPayload remote, string text, CancellationToken ct)
    {
        remote = await PrepareRemoteAsync(remote, ct);
        var now = DateTimeOffset.UtcNow;
        var expires = now.Add(ProtocolConstants.TextSnippetLifetime);
        var request = ProtocolRequestFactory.CreateText(
            remote.Nonce,
            _identity.DeviceId,
            _identity.DeviceName,
            text,
            expires,
            now);

        var client = new TlsPeerClient();
        await using var ssl = await client.ConnectAsync(remote.Host, remote.Port, remote.CertificateFingerprint, _identity.Certificate, ct);
        await FrameProtocol.WriteJsonAsync(ssl, request, ct);
        var response = await FrameProtocol.ReadJsonAsync<TransferAcknowledgement>(ssl, ct);
        TransferResponsePolicy.ValidateTextAcknowledgement(
            response.Accepted,
            response.ResumeOffset,
            response.Message);
    }

    private async Task<PairingPayload> PrepareRemoteAsync(PairingPayload remote, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(remote);
        ct.ThrowIfCancellationRequested();
        await _identity.InitializeAsync();
        return PairingCodec.Validate(remote, DateTimeOffset.UtcNow);
    }

    private sealed class ForwardingProgress<T> : IProgress<T>
    {
        private readonly Action<T> _report;

        public ForwardingProgress(Action<T> report)
        {
            _report = report;
        }

        public void Report(T value) => _report(value);
    }
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
    string TransferId,
    IReadOnlyList<FileTransferSource> Completed,
    IReadOnlyList<FileTransferSource> Skipped);
