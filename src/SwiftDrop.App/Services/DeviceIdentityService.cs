using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;

namespace SwiftDrop.App.Services;

public sealed class DeviceIdentityService : IAsyncDisposable
{
    private const string DeviceIdKey = "device_id";
    private const string DeviceNameKey = "device_name";
    private const string CertificateKey = "device_certificate";

    private readonly ConcurrentDictionary<string, DateTimeOffset> _activePairingNonces = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _initializeGate = new(1, 1);
    private X509Certificate2? _certificate;
    private bool _initialized;

    public string DeviceId { get; private set; } = string.Empty;
    public string DeviceName { get; private set; } = string.Empty;
    public X509Certificate2 Certificate => _certificate ?? throw new InvalidOperationException("Identity not initialized.");

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        await _initializeGate.WaitAsync();
        try
        {
            if (_initialized) return;
            DeviceId = Preferences.Default.Get(DeviceIdKey, string.Empty);
            if (string.IsNullOrWhiteSpace(DeviceId))
            {
                DeviceId = Guid.NewGuid().ToString("N");
                Preferences.Default.Set(DeviceIdKey, DeviceId);
            }

            DeviceName = NormalizeDeviceName(Preferences.Default.Get(DeviceNameKey, DeviceInfo.Current.Name));
            Preferences.Default.Set(DeviceNameKey, DeviceName);

            var pfx = await SecureStorage.Default.GetAsync(CertificateKey);
            if (string.IsNullOrWhiteSpace(pfx))
            {
                _certificate = new CertificateService().CreateSelfSigned(DeviceId);
                await PersistCertificateAsync(_certificate);
            }
            else
            {
                _certificate = new X509Certificate2(Convert.FromBase64String(pfx));
            }

            _initialized = true;
        }
        finally
        {
            _initializeGate.Release();
        }
    }

    public async Task RenameAsync(string newName)
    {
        await InitializeAsync();
        var normalized = NormalizeDeviceName(newName);
        Preferences.Default.Set(DeviceNameKey, normalized);
        DeviceName = normalized;
    }

    public async Task ResetIdentityAsync()
    {
        await _initializeGate.WaitAsync();
        try
        {
            _activePairingNonces.Clear();
            _certificate?.Dispose();
            _certificate = null;
            SecureStorage.Default.Remove(CertificateKey);
            Preferences.Default.Remove(DeviceIdKey);

            DeviceId = Guid.NewGuid().ToString("N");
            Preferences.Default.Set(DeviceIdKey, DeviceId);
            DeviceName = NormalizeDeviceName(Preferences.Default.Get(DeviceNameKey, DeviceInfo.Current.Name));
            _certificate = new CertificateService().CreateSelfSigned(DeviceId);
            await PersistCertificateAsync(_certificate);
            _initialized = true;
        }
        finally
        {
            _initializeGate.Release();
        }
    }

    public string CreatePairingLink()
    {
        if (!_initialized) throw new InvalidOperationException("Identity not initialized.");
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
        if (string.IsNullOrWhiteSpace(nonce)) return false;
        PruneExpiredNonces();
        return _activePairingNonces.TryRemove(nonce, out var expires) && expires >= DateTimeOffset.UtcNow;
    }

    private async Task PersistCertificateAsync(X509Certificate2 certificate)
    {
        var pfx = certificate.Export(X509ContentType.Pfx);
        try
        {
            await SecureStorage.Default.SetAsync(CertificateKey, Convert.ToBase64String(pfx));
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(pfx);
        }
    }

    private void PruneExpiredNonces()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var item in _activePairingNonces)
            if (item.Value < now) _activePairingNonces.TryRemove(item.Key, out _);
    }

    private static string NormalizeDeviceName(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0) trimmed = "SwiftDrop device";
        if (trimmed.Length > 64) trimmed = trimmed[..64];
        return new string(trimmed.Where(ch => !char.IsControl(ch)).ToArray());
    }

    private static string GetLanAddress()
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
        foreach (var unicast in nic.GetIPProperties().UnicastAddresses)
            if (unicast.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(unicast.Address)) return unicast.Address.ToString();
        return "127.0.0.1";
    }

    public ValueTask DisposeAsync()
    {
        _activePairingNonces.Clear();
        _certificate?.Dispose();
        _certificate = null;
        _initializeGate.Dispose();
        return ValueTask.CompletedTask;
    }
}
