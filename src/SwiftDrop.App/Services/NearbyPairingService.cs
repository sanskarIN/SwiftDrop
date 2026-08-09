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
        var expectedFingerprint = Fingerprint.NormalizeSha256(peer.CertificateFingerprint);
        var host = LocalAddressPolicy.ParseAndValidate(peer.Host).ToString();
        if (peer.Port is < 1 or > 65_535)
            throw new InvalidDataException("The discovered device advertised an invalid port.");
        ValidatePairingCode(pairingCode, required: false);

        await _identity.InitializeAsync();

        var client = new TlsPeerClient();
        await using var stream = await client.ConnectAsync(
            host,
            peer.Port,
            expectedFingerprint,
            _identity.Certificate,
            ct);
        await WritePairingRequestAsync(stream, pairingCode, ct);

        var response = await FrameProtocol.ReadJsonAsync<PairingResponse>(stream, ct);
        if (!response.Accepted || string.IsNullOrWhiteSpace(response.PairingLink))
            throw new IOException(response.Message ?? "The nearby device declined pairing.");

        var payload = PairingCodec.Decode(response.PairingLink);
        if (!string.Equals(payload.DeviceId, peer.Id, StringComparison.Ordinal))
            throw new InvalidDataException("Nearby device identity changed during pairing.");
        if (!Fingerprint.FixedTimeEquals(payload.CertificateFingerprint, expectedFingerprint))
            throw new InvalidDataException("Nearby device certificate changed during pairing.");
        if (!IPAddress.TryParse(payload.Host, out var payloadAddress) ||
            !IPAddress.TryParse(host, out var discoveryAddress) ||
            !payloadAddress.Equals(discoveryAddress))
            throw new InvalidDataException("Nearby device address changed during pairing.");
        if (payload.Port != peer.Port)
            throw new InvalidDataException("Nearby device port changed during pairing.");
        return payload;
    }

    public async Task<PairingPayload> RequestManualIpAsync(
        string host,
        int port,
        string pairingCode,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        var validatedHost = LocalAddressPolicy.ParseAndValidate(host.Trim('[', ']')).ToString();
        if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        ValidatePairingCode(pairingCode, required: true);

        await _identity.InitializeAsync();
        var client = new TlsPeerClient();
        await using var bootstrap = await client.ConnectUnpinnedBootstrapAsync(
            validatedHost,
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
        if (!IPAddress.TryParse(payload.Host, out var payloadAddress) ||
            !IPAddress.TryParse(validatedHost, out var requestedAddress) ||
            !payloadAddress.Equals(requestedAddress))
            throw new InvalidDataException("The manual pairing response changed the receiver address.");
        if (payload.Port != port)
            throw new InvalidDataException("The manual pairing response changed the receiver port.");
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
