using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Protocol;

public static class ProtocolRequestFactory
{
    public static ProtocolRequest CreateFile(
        string pairingNonce,
        string senderDeviceId,
        string senderDeviceName,
        FileManifestEntry entry)
    {
        ValidateCommon(pairingNonce, senderDeviceId, senderDeviceName, "file");
        var validated = ManifestValidator.ValidateEntry(entry ?? throw new ArgumentNullException(nameof(entry)));
        return new ProtocolRequest(
            "file",
            ProtocolConstants.CurrentVersion,
            pairingNonce,
            senderDeviceId,
            senderDeviceName,
            Entry: validated);
    }

    public static ProtocolRequest CreateBatch(
        string pairingNonce,
        string senderDeviceId,
        string senderDeviceName,
        string transferId,
        IReadOnlyList<FileManifestEntry> files,
        long totalBytes)
    {
        ValidateCommon(pairingNonce, senderDeviceId, senderDeviceName, "batch");
        IncomingRequestPolicy.ValidateTransferId(transferId);
        var validated = BatchManifestValidator.Validate(files ?? throw new ArgumentNullException(nameof(files)), totalBytes);
        return new ProtocolRequest(
            "batch",
            ProtocolConstants.CurrentVersion,
            pairingNonce,
            senderDeviceId,
            senderDeviceName,
            TransferId: transferId,
            Files: validated,
            TotalBytes: totalBytes);
    }

    public static ProtocolRequest CreateText(
        string pairingNonce,
        string senderDeviceId,
        string senderDeviceName,
        string text,
        DateTimeOffset expiresUtc,
        DateTimeOffset nowUtc)
    {
        ValidateCommon(pairingNonce, senderDeviceId, senderDeviceName, "text");
        TextSnippetValidator.Validate(text, expiresUtc, nowUtc);
        return new ProtocolRequest(
            "text",
            ProtocolConstants.CurrentVersion,
            pairingNonce,
            senderDeviceId,
            senderDeviceName,
            Text: text,
            ExpiresUnixSeconds: expiresUtc.ToUnixTimeSeconds());
    }

    public static ProtocolRequest CreatePairRequest(
        string senderDeviceId,
        string senderDeviceName,
        string? pairingCode = null)
    {
        IncomingRequestPolicy.ValidateEnvelope(ProtocolConstants.CurrentVersion, "pair-request");
        IncomingRequestPolicy.ValidateSenderIdentity(senderDeviceId, senderDeviceName);
        if (pairingCode is not null && (pairingCode.Length != 8 || pairingCode.Any(ch => ch is < '0' or > '9')))
            throw new InvalidDataException("Pairing code must contain exactly 8 digits.");
        return new ProtocolRequest(
            "pair-request",
            ProtocolConstants.CurrentVersion,
            null,
            senderDeviceId,
            senderDeviceName,
            PairingCode: pairingCode);
    }

    private static void ValidateCommon(
        string pairingNonce,
        string senderDeviceId,
        string senderDeviceName,
        string type)
    {
        IncomingRequestPolicy.ValidateEnvelope(ProtocolConstants.CurrentVersion, type);
        IncomingRequestPolicy.ValidateSenderIdentity(senderDeviceId, senderDeviceName);
        ValidatePairingNonce(pairingNonce);
    }

    private static void ValidatePairingNonce(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length is < 16 or > 128 ||
            value.Any(ch => !char.IsAsciiLetterOrDigit(ch) && ch is not '-' and not '_'))
            throw new InvalidDataException("Pairing authorization nonce is invalid.");
    }
}
