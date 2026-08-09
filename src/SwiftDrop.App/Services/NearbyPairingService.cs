using System.Net;
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
        ValidatePairingCode(pairingCode, required: false);

        await _identity.InitializeAsync();

        var client = new TlsPeerClient();
        await using var stream = await client.ConnectAsync(
            peer.Host,
            peer.Port,
            peer.CertificateFingerprint,
            _identity.Certificate,
            ct);
        await WritePairingRequestAsync(stream, pairingCode, ct);

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

    public async Task<PairingPayload> RequestManualIpAsync(
        string host,
        int port,
        string pairingCode,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        if (!IPAddress.TryParse(host.Trim('[', ']'), out _))
            throw new ArgumentException("Enter a numeric IPv4 or IPv6 address.", nameof(host));
        if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        ValidatePairingCode(pairingCode, required: true);

        await _identity.InitializeAsync();
        var client = new TlsPeerClient();
        await using var bootstrap = await client.ConnectUnpinnedBootstrapAsync(
            host.Trim('[', ']'),
            port,
            _identity.Certificate,
            ct);
        await WritePairingRequestAsync(bootstrap.Stream, pairingCode, ct);
        var response = await FrameProtocol.ReadJsonAsync<PairingResponse>(bootstrap.Stream, ct);
        if (!response.Accepted || string.IsNullOrWhiteSpace(response.PairingLink))
            throw new IOException(response.Message ?? "The receiving device declined manual pairing.");

        var payload = PairingCodec.Decode(response.PairingLink);
        if (!Fingerprint.FixedTimeEquals(payload.CertificateFingerprint, bootstrap.ServerFingerprint))
            throw new InvalidDataException("The pairing response certificate does not match the TLS bootstrap certificate.");
        return payload;
    }

    private Task WritePairingRequestAsync(Stream stream, string? pairingCode, CancellationToken ct)
        => FrameProtocol.WriteJsonAsync(stream, new
        {
            type = "pair-request",
            protocolVersion = ProtocolConstants.CurrentVersion,
            senderDeviceId = _identity.DeviceId,
            senderDeviceName = _identity.DeviceName,
            pairingCode
        }, ct);

    private static void ValidatePairingCode(string? pairingCode, bool required)
    {
        if (string.IsNullOrWhiteSpace(pairingCode))
        {
            if (required) throw new ArgumentException("An eight-digit pairing code is required.", nameof(pairingCode));
            return;
        }
        if (pairingCode.Length != 8 || pairingCode.Any(ch => ch is < '0' or > '9'))
            throw new ArgumentException("Pairing code must contain exactly eight digits.", nameof(pairingCode));
    }

    private sealed record PairingResponse(bool Accepted, string? Message, string? PairingLink);
}
