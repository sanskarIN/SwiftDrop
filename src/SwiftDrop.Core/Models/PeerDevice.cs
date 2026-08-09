namespace SwiftDrop.Core.Models;

public sealed record PeerDevice(
    string Id,
    string Name,
    string Platform,
    string Host,
    int Port,
    string? CertificateFingerprint = null,
    bool IsTrusted = false,
    DateTimeOffset? LastSeenUtc = null);
