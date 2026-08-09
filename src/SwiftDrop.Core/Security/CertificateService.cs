using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SwiftDrop.Core.Security;

public sealed class CertificateService
{
    public X509Certificate2 CreateSelfSigned(string deviceId)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest($"CN=SwiftDrop-{deviceId}", key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddYears(5));
        return new X509Certificate2(cert.Export(X509ContentType.Pfx));
    }
}
