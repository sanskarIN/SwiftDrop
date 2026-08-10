using SwiftDrop.Core.Models;

namespace SwiftDrop.App.Services;

internal static class TransferCoordinatorCompatibilityExtensions
{
    public static Task<BatchSendResult> SendBatchAsync(
        this TransferCoordinator coordinator,
        PairingPayload remote,
        IEnumerable<string> paths,
        IProgress<BatchProgress>? progress,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(coordinator);
        return coordinator.SendBatchAsync(
            remote,
            paths,
            Guid.NewGuid().ToString("N"),
            progress,
            ct);
    }
}
