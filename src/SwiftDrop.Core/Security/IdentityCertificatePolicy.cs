using System.Security.Cryptography.X509Certificates;

namespace SwiftDrop.Core.Security;

public static class IdentityCertificatePolicy
{
    public static readonly TimeSpan RenewalWindow = TimeSpan.FromDays(7);
    public static readonly TimeSpan ClockSkewTolerance = TimeSpan.FromMinutes(10);

    public static IdentityCertificateStatus Evaluate(X509Certificate2 certificate, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        if (!certificate.HasPrivateKey)
            return new IdentityCertificateStatus(false, IdentityCertificateIssue.MissingPrivateKey);

        var notBefore = new DateTimeOffset(certificate.NotBefore.ToUniversalTime());
        var notAfter = new DateTimeOffset(certificate.NotAfter.ToUniversalTime());
        if (notBefore > nowUtc.Add(ClockSkewTolerance))
            return new IdentityCertificateStatus(false, IdentityCertificateIssue.NotYetValid);
        if (notAfter <= nowUtc)
            return new IdentityCertificateStatus(false, IdentityCertificateIssue.Expired);
        if (notAfter <= nowUtc.Add(RenewalWindow))
            return new IdentityCertificateStatus(false, IdentityCertificateIssue.NearExpiry);

        using var ecdsa = certificate.GetECDsaPrivateKey();
        if (ecdsa is null)
            return new IdentityCertificateStatus(false, IdentityCertificateIssue.UnsupportedPrivateKey);

        return new IdentityCertificateStatus(true, IdentityCertificateIssue.None);
    }
}

public readonly record struct IdentityCertificateStatus(bool IsUsable, IdentityCertificateIssue Issue);

public enum IdentityCertificateIssue
{
    None,
    MissingPrivateKey,
    NotYetValid,
    Expired,
    NearExpiry,
    UnsupportedPrivateKey,
    CorruptStoredCertificate
}
