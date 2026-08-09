namespace SwiftDrop.Core.Models;

public sealed record PairingPayload(
    string Version,
    string DeviceId,
    string DeviceName,
    string Host,
    int Port,
    string CertificateFingerprint,
    string Nonce,
    long ExpiresUnixSeconds);

public sealed record TrustedPeer(
    string DeviceId,
    string DeviceName,
    string CertificateFingerprint,
    DateTimeOffset TrustedAtUtc,
    DateTimeOffset LastSeenUtc);
