using System.Security.Cryptography.X509Certificates;
using SwiftDrop.Core.Security;
using Xunit;

namespace SwiftDrop.Core.Tests;

public sealed class CertificateServiceTests
{
    [Fact]
    public void CreateSelfSigned_ProducesCurrentEcdsaTlsIdentity()
    {
        using var certificate = new CertificateService().CreateSelfSigned("certificate-test");

        Assert.True(certificate.HasPrivateKey);
        using var ecdsa = certificate.GetECDsaPrivateKey();
        Assert.NotNull(ecdsa);
        Assert.True(certificate.NotAfter.ToUniversalTime() > DateTime.UtcNow.AddYears(4));
        Assert.True(IdentityCertificatePolicy.Evaluate(certificate, DateTimeOffset.UtcNow).IsUsable);

        var keyUsage = certificate.Extensions.OfType<X509KeyUsageExtension>().Single();
        Assert.True(keyUsage.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature));

        var eku = certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>().Single();
        var values = eku.EnhancedKeyUsages.Cast<Oid>().Select(x => x.Value).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("1.3.6.1.5.5.7.3.1", values);
        Assert.Contains("1.3.6.1.5.5.7.3.2", values);
    }

    [Fact]
    public void CreateSelfSigned_RejectsOversizedDeviceId()
    {
        Assert.Throws<ArgumentException>(() =>
            new CertificateService().CreateSelfSigned(new string('a', 129)));
    }
}
