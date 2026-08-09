using System.Text;
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

    public async Task SendTextAsync(PairingPayload remote, string text, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(remote);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var byteCount = Encoding.UTF8.GetByteCount(text);
        if (byteCount > ProtocolConstants.MaxTextBytes)
            throw new InvalidDataException($"Text snippet exceeds {ProtocolConstants.MaxTextBytes:N0} UTF-8 bytes.");

        var expires = DateTimeOffset.UtcNow.Add(ProtocolConstants.TextSnippetLifetime);
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
}
