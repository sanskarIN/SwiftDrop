namespace SwiftDrop.Core.Models;

public sealed record ProtocolRequest(
    string Type,
    string ProtocolVersion,
    string? PairingNonce,
    string? SenderDeviceId,
    string? SenderDeviceName,
    FileManifestEntry? Entry = null,
    string? Text = null,
    long? ExpiresUnixSeconds = null,
    string? PairingCode = null,
    string? TransferId = null,
    IReadOnlyList<FileManifestEntry>? Files = null,
    long? TotalBytes = null);

public sealed record TransferAcknowledgement(
    bool Accepted,
    long ResumeOffset,
    string? Message = null);

public sealed record BatchItemStart(string RelativePath);

public sealed record PairingResponse(
    bool Accepted,
    string? Message,
    string? PairingLink);
