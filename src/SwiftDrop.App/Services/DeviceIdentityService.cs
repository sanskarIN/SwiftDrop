using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;

namespace SwiftDrop.App.Services;

public sealed class DeviceIdentityService
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _activePairingNonces = new(StringComparer.Ordinal);
    private X509Certificate2? _certificate;
    public string DeviceId { get; private set; } = string.Empty;
    public string DeviceName { get; private set; } = string.Empty;
    public X509Certificate2 Certificate => _certificate ?? throw new InvalidOperationException("Identity not initialized.");

    public async Task InitializeAsync()
    {
        DeviceId = Preferences.Default.Get("device_id", string.Empty);
        if (string.IsNullOrWhiteSpace(DeviceId))
        {
            DeviceId = Guid.NewGuid().ToString("N");
            Preferences.Default.Set("device_id", DeviceId);
        }
        DeviceName = Preferences.Default.Get("device_name", DeviceInfo.Current.Name);
        var pfx = await SecureStorage.Default.GetAsync("device_certificate");
        if (string.IsNullOrWhiteSpace(pfx))
        {
            _certificate = new CertificateService().CreateSelfSigned(DeviceId);
            await SecureStorage.Default.SetAsync("device_certificate", Convert.ToBase64String(_certificate.Export(X509ContentType.Pfx)));
        }
        else _certificate = new X509Certificate2(Convert.FromBase64String(pfx));
    }

    public string CreatePairingLink()
    {
        PruneExpiredNonces();
        var expires = DateTimeOffset.UtcNow.Add(ProtocolConstants.PairingLifetime);
        var nonce = PairingCodec.CreateNonce();
        _activePairingNonces[nonce] = expires;
        var payload = new PairingPayload(
            ProtocolConstants.CurrentVersion,
            DeviceId,
            DeviceName,
            GetLanAddress(),
            ProtocolConstants.DefaultPort,
            Fingerprint.FromCertificate(Certificate),
            nonce,
            expires.ToUnixTimeSeconds());
        return PairingCodec.Encode(payload);
    }

    public bool TryConsumePairingNonce(string nonce)
    {
        PruneExpiredNonces();
        return _activePairingNonces.TryRemove(nonce, out var expires) && expires >= DateTimeOffset.UtcNow;
    }

    private void PruneExpiredNonces()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var item in _activePairingNonces)
            if (item.Value < now) _activePairingNonces.TryRemove(item.Key, out _);
    }

    private static string GetLanAddress()
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
        foreach (var unicast in nic.GetIPProperties().UnicastAddresses)
            if (unicast.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(unicast.Address)) return unicast.Address.ToString();
        return "127.0.0.1";
    }
}
