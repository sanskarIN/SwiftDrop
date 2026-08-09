using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SwiftDrop.Core.Security;

public sealed class CertificateService
{
    private static readonly Oid ServerAuthenticationOid = new("1.3.6.1.5.5.7.3.1", "Server Authentication");
    private static readonly Oid ClientAuthenticationOid = new("1.3.6.1.5.5.7.3.2", "Client Authentication");

    public X509Certificate2 CreateSelfSigned(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        if (deviceId.Length > 128 || deviceId.Any(char.IsControl))
            throw new ArgumentException("Device ID is too long or contains control characters.", nameof(deviceId));

        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var escapedDeviceId = deviceId.Replace("\\", "\\\\", StringComparison.Ordinal).Replace(",", "\\,", StringComparison.Ordinal);
        var request = new CertificateRequest($"CN=SwiftDrop-{escapedDeviceId}", key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { ServerAuthenticationOid, ClientAuthenticationOid },
            true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddYears(5));
        var pfx = certificate.Export(X509ContentType.Pfx);
        try
        {
            return X509CertificateLoader.LoadPkcs12(pfx, null);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pfx);
        }
    }
}
