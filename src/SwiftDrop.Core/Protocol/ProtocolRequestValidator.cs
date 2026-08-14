using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Protocol;

public static class ProtocolRequestValidator
{
    public static ProtocolRequest Validate(ProtocolRequest request, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(request);
        IncomingRequestPolicy.ValidateEnvelope(request.ProtocolVersion, request.Type);
        IncomingRequestPolicy.ValidateSenderIdentity(request.SenderDeviceId, request.SenderDeviceName);

        switch (request.Type)
        {
            case "file":
                ValidateTransferAuthorization(request.PairingNonce);
                RequireNull(request.Text, request.ExpiresUnixSeconds, request.PairingCode, request.TransferId, request.Files, request.TotalBytes);
                if (request.Entry is null) throw new InvalidDataException("File metadata is required.");
                ManifestValidator.ValidateEntry(request.Entry);
                break;

            case "batch":
                ValidateTransferAuthorization(request.PairingNonce);
                RequireNull(request.Entry, request.Text, request.ExpiresUnixSeconds, request.PairingCode);
                IncomingRequestPolicy.ValidateTransferId(request.TransferId);
                if (request.Files is null || request.TotalBytes is null)
                    throw new InvalidDataException("Batch metadata is required.");
                BatchManifestValidator.Validate(request.Files, request.TotalBytes);
                break;

            case "text":
                ValidateTransferAuthorization(request.PairingNonce);
                RequireNull(request.Entry, request.PairingCode, request.TransferId, request.Files, request.TotalBytes);
                if (request.ExpiresUnixSeconds is null)
                    throw new InvalidDataException("Text expiration is required.");
                DateTimeOffset expiresUtc;
                try
                {
                    expiresUtc = DateTimeOffset.FromUnixTimeSeconds(request.ExpiresUnixSeconds.Value);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    throw new InvalidDataException("Text expiration is invalid.", ex);
                }
                TextSnippetValidator.Validate(request.Text, expiresUtc, nowUtc);
                break;

            case "pair-request":
                if (request.PairingNonce is not null)
                    throw new InvalidDataException("Pair requests cannot carry transfer authorization.");
                RequireNull(request.Entry, request.Text, request.ExpiresUnixSeconds, request.TransferId, request.Files, request.TotalBytes);
                IncomingRequestPolicy.ValidatePairingCode(request.PairingCode, required: false);
                break;

            default:
                throw new InvalidDataException("Unsupported transfer request type.");
        }

        return request;
    }

    private static void ValidateTransferAuthorization(string? pairingNonce)
        => IncomingRequestPolicy.ValidatePairingNonce(pairingNonce);

    private static void RequireNull(params object?[] values)
    {
        if (values.Any(value => value is not null))
            throw new InvalidDataException("Protocol request contains fields that are not valid for its request type.");
    }
}
