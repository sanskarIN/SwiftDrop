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
