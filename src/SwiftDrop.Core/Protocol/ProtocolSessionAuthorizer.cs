using SwiftDrop.Core.Models;

namespace SwiftDrop.Core.Protocol;

public static class ProtocolSessionAuthorizer
{
    public static ProtocolRequest ValidateAndAuthorize(
        ProtocolRequest request,
        DateTimeOffset nowUtc,
        Func<string, bool> consumeAuthorization)
    {
        ArgumentNullException.ThrowIfNull(consumeAuthorization);
        ProtocolRequestValidator.Validate(request, nowUtc);

        if (string.Equals(request.Type, "pair-request", StringComparison.Ordinal))
            return request;

        var nonce = IncomingRequestPolicy.ValidatePairingNonce(request.PairingNonce);
        if (!consumeAuthorization(nonce))
            throw new UnauthorizedAccessException("Pairing authorization failed.");
        return request;
    }
}
