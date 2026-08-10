namespace SwiftDrop.Core.Protocol;

public static class IncomingRequestPolicy
{
    private static readonly HashSet<string> AllowedRequestTypes = new(StringComparer.Ordinal)
    {
        "file",
        "batch",
        "text",
        "pair-request"
    };

    public static void ValidateEnvelope(string? protocolVersion, string? requestType)
    {
        if (!string.Equals(protocolVersion, ProtocolConstants.CurrentVersion, StringComparison.Ordinal))
            throw new NotSupportedException("Unsupported protocol version.");
        if (string.IsNullOrWhiteSpace(requestType) || !AllowedRequestTypes.Contains(requestType))
            throw new InvalidDataException("Unsupported transfer request type.");
    }

    public static void ValidateSenderIdentity(string? deviceId, string? deviceName)
    {
        if (!IsBoundedIdentity(deviceId, 128))
            throw new InvalidDataException("Invalid sender device ID.");
        if (!IsBoundedIdentity(deviceName, 128))
            throw new InvalidDataException("Invalid sender device name.");
    }

    public static string ValidatePairingNonce(string? pairingNonce)
    {
        if (string.IsNullOrWhiteSpace(pairingNonce) ||
            pairingNonce.Length is < 16 or > 128 ||
            pairingNonce.Any(ch => !char.IsAsciiLetterOrDigit(ch) && ch is not '-' and not '_'))
            throw new InvalidDataException("Pairing authorization nonce is invalid.");
        return pairingNonce;
    }

    public static string ValidatePairingCode(string? pairingCode, bool required)
    {
        if (string.IsNullOrWhiteSpace(pairingCode))
        {
            if (required) throw new InvalidDataException("An eight-digit pairing code is required.");
            return string.Empty;
        }
        if (pairingCode.Length != 8 || pairingCode.Any(ch => ch is < '0' or > '9'))
            throw new InvalidDataException("Pairing code must contain exactly eight digits.");
        return pairingCode;
    }

    public static string ValidateTransferId(string? transferId)
    {
        if (string.IsNullOrWhiteSpace(transferId) || transferId.Length > 128 || transferId.Any(char.IsControl))
            throw new InvalidDataException("Invalid transfer identifier.");
        return transferId;
    }

    public static void ValidateBatchItemStart(string? expectedRelativePath, string? actualRelativePath)
    {
        if (string.IsNullOrWhiteSpace(expectedRelativePath) || string.IsNullOrWhiteSpace(actualRelativePath) ||
            !string.Equals(expectedRelativePath, actualRelativePath, StringComparison.Ordinal))
            throw new InvalidDataException("Batch item order or path did not match the negotiated plan.");
    }

    private static bool IsBoundedIdentity(string? value, int maxLength)
        => !string.IsNullOrWhiteSpace(value) &&
           value.Length <= maxLength &&
           !value.Any(char.IsControl);
}
