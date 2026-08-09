using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using SwiftDrop.Core.Security;
using Xunit;

namespace SwiftDrop.Core.Tests;

public sealed class IdentityCertificatePolicyTests
{
    [Fact]
    public void Evaluate_AcceptsCurrentEcdsaIdentityCertificate()
    {
        using var certificate = new CertificateService().CreateSelfSigned("policy-current");
        var status = IdentityCertificatePolicy.Evaluate(certificate, DateTimeOffset.UtcNow);
        Assert.True(status.IsUsable);
        Assert.Equal(IdentityCertificateIssue.None, status.Issue);
    }

    [Fact]
    public void Evaluate_RejectsCertificateWithoutPrivateKey()
    {
        using var withKey = new CertificateService().CreateSelfSigned("policy-public-only");
        using var publicOnly = X509CertificateLoader.LoadCertificate(withKey.RawData);
        var status = IdentityCertificatePolicy.Evaluate(publicOnly, DateTimeOffset.UtcNow);
        Assert.False(status.IsUsable);
        Assert.Equal(IdentityCertificateIssue.MissingPrivateKey, status.Issue);
    }

    [Fact]
    public void Evaluate_RequestsRenewalInsideSevenDayWindow()
    {
        var now = DateTimeOffset.UtcNow;
        using var certificate = CreateEcdsaCertificate(now.AddDays(-1), now.AddDays(3));
        var status = IdentityCertificatePolicy.Evaluate(certificate, now);
        Assert.False(status.IsUsable);
        Assert.Equal(IdentityCertificateIssue.NearExpiry, status.Issue);
    }

    [Fact]
    public void Evaluate_RejectsExpiredCertificate()
    {
        var now = DateTimeOffset.UtcNow;
        using var certificate = CreateEcdsaCertificate(now.AddDays(-10), now.AddDays(-1));
        var status = IdentityCertificatePolicy.Evaluate(certificate, now);
        Assert.False(status.IsUsable);
        Assert.Equal(IdentityCertificateIssue.Expired, status.Issue);
    }

    [Fact]
    public void Evaluate_RejectsUnsupportedPrivateKeyAlgorithm()
    {
        var now = DateTimeOffset.UtcNow;
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=SwiftDrop-rsa-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var temporary = request.CreateSelfSigned(now.AddMinutes(-5), now.AddDays(30));
        var pfx = temporary.Export(X509ContentType.Pfx);
        try
        {
            using var certificate = X509CertificateLoader.LoadPkcs12(pfx, null);
            var status = IdentityCertificatePolicy.Evaluate(certificate, now);
            Assert.False(status.IsUsable);
            Assert.Equal(IdentityCertificateIssue.UnsupportedPrivateKey, status.Issue);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pfx);
        }
    }

    private static X509Certificate2 CreateEcdsaCertificate(DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=SwiftDrop-policy-test", key, HashAlgorithmName.SHA256);
        using var temporary = request.CreateSelfSigned(notBefore, notAfter);
        var pfx = temporary.Export(X509ContentType.Pfx);
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
