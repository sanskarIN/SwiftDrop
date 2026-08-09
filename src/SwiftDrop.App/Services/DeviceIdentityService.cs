using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
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
    public bool IdentityWasAutomaticallyRegenerated { get; private set; }
    public IdentityCertificateIssue? AutomaticRegenerationReason { get; private set; }

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
            await LoadOrCreateCertificateAsync();
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
            await RegenerateIdentityAsync(automatic: false, reason: null);
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

    private async Task LoadOrCreateCertificateAsync()
    {
        var stored = await SecureStorage.Default.GetAsync(CertificateKey);
        if (string.IsNullOrWhiteSpace(stored))
        {
            _certificate = new CertificateService().CreateSelfSigned(DeviceId);
            await PersistCertificateAsync(_certificate);
            return;
        }

        byte[]? bytes = null;
        X509Certificate2? candidate = null;
        IdentityCertificateIssue? regenerationReason = null;
        try
        {
            bytes = Convert.FromBase64String(stored);
            candidate = X509CertificateLoader.LoadPkcs12(bytes, null);
            var status = IdentityCertificatePolicy.Evaluate(candidate, DateTimeOffset.UtcNow);
            if (status.IsUsable)
            {
                _certificate = candidate;
                candidate = null;
                return;
            }
            regenerationReason = status.Issue;
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            regenerationReason = IdentityCertificateIssue.CorruptStoredCertificate;
        }
        finally
        {
            candidate?.Dispose();
            if (bytes is not null) CryptographicOperations.ZeroMemory(bytes);
        }

        await RegenerateIdentityAsync(automatic: true, regenerationReason ?? IdentityCertificateIssue.CorruptStoredCertificate);
    }

    private async Task RegenerateIdentityAsync(bool automatic, IdentityCertificateIssue? reason)
    {
        _activePairingNonces.Clear();
        _certificate?.Dispose();
        _certificate = null;
        SecureStorage.Default.Remove(CertificateKey);

        DeviceId = Guid.NewGuid().ToString("N");
        Preferences.Default.Set(DeviceIdKey, DeviceId);
        DeviceName = NormalizeDeviceName(Preferences.Default.Get(DeviceNameKey, DeviceInfo.Current.Name));
        Preferences.Default.Set(DeviceNameKey, DeviceName);

        _certificate = new CertificateService().CreateSelfSigned(DeviceId);
        await PersistCertificateAsync(_certificate);
        IdentityWasAutomaticallyRegenerated = automatic;
        AutomaticRegenerationReason = automatic ? reason : null;
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
            CryptographicOperations.ZeroMemory(pfx);
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
        var clean = new string(trimmed.Where(ch => !char.IsControl(ch)).ToArray()).Trim();
        if (clean.Length == 0) clean = "SwiftDrop device";
        if (clean.Length > 64) clean = clean[..64].TrimEnd();
        return clean;
    }

    private static string GetLanAddress()
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()
                     .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
        {
            foreach (var unicast in nic.GetIPProperties().UnicastAddresses)
            {
                if (unicast.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(unicast.Address))
                    return unicast.Address.ToString();
            }
        }
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
