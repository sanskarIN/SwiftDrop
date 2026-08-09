using SwiftDrop.Core.Models;
using SwiftDrop.Core.Networking;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.App.Services;

public sealed class NearbyPairingService
{
    private readonly DeviceIdentityService _identity;

    public NearbyPairingService(DeviceIdentityService identity)
    {
        _identity = identity;
    }

    public async Task<PairingPayload> RequestAsync(
        PeerDevice peer,
        string? pairingCode = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(peer);
        if (string.IsNullOrWhiteSpace(peer.CertificateFingerprint))
            throw new InvalidOperationException("The discovered device did not advertise a certificate fingerprint. Use QR pairing instead.");
        if (pairingCode is not null && (pairingCode.Length != 8 || pairingCode.Any(ch => ch is < '0' or > '9')))
            throw new ArgumentException("Pairing code must contain exactly eight digits.", nameof(pairingCode));

        await _identity.InitializeAsync();

        var client = new TlsPeerClient();
        await using var stream = await client.ConnectAsync(
            peer.Host,
            peer.Port,
            peer.CertificateFingerprint,
            _identity.Certificate,
            ct);
        await FrameProtocol.WriteJsonAsync(stream, new
        {
            type = "pair-request",
            protocolVersion = ProtocolConstants.CurrentVersion,
            senderDeviceId = _identity.DeviceId,
            senderDeviceName = _identity.DeviceName,
            pairingCode
        }, ct);

        var response = await FrameProtocol.ReadJsonAsync<PairingResponse>(stream, ct);
        if (!response.Accepted || string.IsNullOrWhiteSpace(response.PairingLink))
            throw new IOException(response.Message ?? "The nearby device declined pairing.");

        var payload = PairingCodec.Decode(response.PairingLink);
        if (!string.Equals(payload.DeviceId, peer.Id, StringComparison.Ordinal))
            throw new InvalidDataException("Nearby device identity changed during pairing.");
        if (!Fingerprint.FixedTimeEquals(payload.CertificateFingerprint, peer.CertificateFingerprint))
            throw new InvalidDataException("Nearby device certificate changed during pairing.");
        return payload;
    }

    private sealed record PairingResponse(bool Accepted, string? Message, string? PairingLink);
}
