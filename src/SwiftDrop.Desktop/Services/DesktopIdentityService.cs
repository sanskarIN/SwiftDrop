using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;

namespace SwiftDrop.Desktop.Services;

public sealed class DesktopIdentityService : IAsyncDisposable
{
    private readonly string _settingsPath = Path.Combine(DesktopPaths.ConfigRoot, "identity.json");
    private readonly string _certificatePath = Path.Combine(DesktopPaths.ConfigRoot, "identity.pfx");
    private readonly OneTimeAuthorizationStore _pairingAuthorizations = new(1024);
    private readonly OneTimePairingCodeManager _pairingCodes = new();
    private readonly SemaphoreSlim _initializeGate = new(1, 1);
    private X509Certificate2? _certificate;
    private bool _initialized;

    public string DeviceId { get; private set; } = string.Empty;
    public string DeviceName { get; private set; } = string.Empty;
    public X509Certificate2 Certificate => _certificate ?? throw new InvalidOperationException("Identity not initialized.");
    public bool IdentityWasAutomaticallyRegenerated { get; private set; }
    public IdentityCertificateIssue? AutomaticRegenerationReason { get; private set; }

    public string PlatformName
        => OperatingSystem.IsLinux() ? "Linux" :
           OperatingSystem.IsWindows() ? "Windows" :
           OperatingSystem.IsMacOS() ? "macOS" : "Desktop";

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        await _initializeGate.WaitAsync();
        try
        {
            if (_initialized) return;
            DesktopPaths.EnsurePrivateDirectory(DesktopPaths.ConfigRoot);
            await LoadSettingsAsync();
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
        DeviceName = NormalizeDeviceName(newName);
        await PersistSettingsAsync();
    }

    public string CreatePairingLink()
    {
        if (!_initialized) throw new InvalidOperationException("Identity not initialized.");
        var now = DateTimeOffset.UtcNow;
        var expires = now.Add(ProtocolConstants.PairingLifetime);
        var nonce = PairingCodec.CreateNonce();
        _pairingAuthorizations.Register(nonce, expires, now);
        return PairingCodec.Encode(new PairingPayload(
            ProtocolConstants.CurrentVersion,
            DeviceId,
            DeviceName,
            GetLanAddress(),
            ProtocolConstants.DefaultPort,
            Fingerprint.FromCertificate(Certificate),
            nonce,
            expires.ToUnixTimeSeconds()));
    }

    public PairingCodeSnapshot CreatePairingCode()
        => _pairingCodes.Create(DateTimeOffset.UtcNow);

    public bool TryConsumePairingNonce(string nonce)
        => _pairingAuthorizations.TryConsume(nonce, DateTimeOffset.UtcNow);

    public bool TryConsumePairingCode(string? code)
        => _pairingCodes.TryConsume(code, DateTimeOffset.UtcNow);

    private async Task LoadSettingsAsync()
    {
        IdentitySettings? settings = null;
        try
        {
            if (File.Exists(_settingsPath))
            {
                var bytes = await File.ReadAllBytesAsync(_settingsPath);
                try { settings = JsonSerializer.Deserialize<IdentitySettings>(bytes); }
                finally { CryptographicOperations.ZeroMemory(bytes); }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            settings = null;
        }

        DeviceId = settings?.DeviceId ?? string.Empty;
        if (!Guid.TryParseExact(DeviceId, "N", out _))
            DeviceId = Guid.NewGuid().ToString("N");

        DeviceName = NormalizeDeviceName(settings?.DeviceName ?? Environment.MachineName);
        await PersistSettingsAsync();
    }

    private async Task PersistSettingsAsync()
    {
        DesktopPaths.EnsurePrivateDirectory(DesktopPaths.ConfigRoot);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new IdentitySettings(DeviceId, DeviceName));
        try
        {
            await File.WriteAllBytesAsync(_settingsPath, bytes);
            DesktopPaths.RestrictPrivateFile(_settingsPath);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    private async Task LoadOrCreateCertificateAsync()
    {
        if (!File.Exists(_certificatePath))
        {
            _certificate = new CertificateService().CreateSelfSigned(DeviceId);
            await PersistCertificateAsync(_certificate);
            return;
        }

        byte[]? bytes = null;
        X509Certificate2? candidate = null;
        IdentityCertificateIssue? reason = null;
        try
        {
            bytes = await File.ReadAllBytesAsync(_certificatePath);
            candidate = X509CertificateLoader.LoadPkcs12(bytes, null);
            var status = IdentityCertificatePolicy.Evaluate(candidate, DateTimeOffset.UtcNow);
            if (status.IsUsable)
            {
                _certificate = candidate;
                candidate = null;
                return;
            }
            reason = status.Issue;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException)
        {
            reason = IdentityCertificateIssue.CorruptStoredCertificate;
        }
        finally
        {
            candidate?.Dispose();
            if (bytes is not null) CryptographicOperations.ZeroMemory(bytes);
        }

        await RegenerateIdentityAsync(reason ?? IdentityCertificateIssue.CorruptStoredCertificate);
    }

    private async Task RegenerateIdentityAsync(IdentityCertificateIssue reason)
    {
        _pairingAuthorizations.Clear();
        _pairingCodes.Invalidate();
        _certificate?.Dispose();
        _certificate = null;
        try { File.Delete(_certificatePath); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }

        DeviceId = Guid.NewGuid().ToString("N");
        await PersistSettingsAsync();
        _certificate = new CertificateService().CreateSelfSigned(DeviceId);
        await PersistCertificateAsync(_certificate);
        IdentityWasAutomaticallyRegenerated = true;
        AutomaticRegenerationReason = reason;
    }

    private async Task PersistCertificateAsync(X509Certificate2 certificate)
    {
        DesktopPaths.EnsurePrivateDirectory(DesktopPaths.ConfigRoot);
        var pfx = certificate.Export(X509ContentType.Pfx);
        try
        {
            await File.WriteAllBytesAsync(_certificatePath, pfx);
            DesktopPaths.RestrictPrivateFile(_certificatePath);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pfx);
        }
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
        return IPAddress.Loopback.ToString();
    }

    public ValueTask DisposeAsync()
    {
        _pairingAuthorizations.Clear();
        _pairingCodes.Invalidate();
        _certificate?.Dispose();
        _certificate = null;
        _initializeGate.Dispose();
        return ValueTask.CompletedTask;
    }

    private sealed record IdentitySettings(string DeviceId, string DeviceName);
}
