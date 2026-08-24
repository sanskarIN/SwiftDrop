namespace SwiftDrop.App.Services;

public static class LocalizedStatusFormatter
{
    public static string QueueCounts(int running, int queued, int interrupted)
        => AppText.Format("RunningCountFormat", running, queued, interrupted);

    public static string QueueTiming(TransferQueueState state, DateTimeOffset? timestamp)
    {
        var value = (timestamp ?? DateTimeOffset.Now).LocalDateTime;
        var key = state switch
        {
            TransferQueueState.Queued => "QueuedAtFormat",
            TransferQueueState.Running => "StartedAtFormat",
            TransferQueueState.Interrupted => "InterruptedAtFormat",
            _ => "FinishedAtFormat"
        };
        return AppText.Format(key, value);
    }

    public static string QueueState(TransferQueueState state)
    {
        var key = state switch
        {
            TransferQueueState.Queued => "QueueStateQueued",
            TransferQueueState.Running => "QueueStateRunning",
            TransferQueueState.Completed => "QueueStateCompleted",
            TransferQueueState.Failed => "QueueStateFailed",
            TransferQueueState.Cancelled => "QueueStateCancelled",
            TransferQueueState.Interrupted => "QueueStateInterrupted",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported queue state.")
        };
        return AppText.Get(key);
    }

    public static string QueueOperation(TransferQueueOperationKind operation)
    {
        var key = operation switch
        {
            TransferQueueOperationKind.Transfer => "QueueOperationTransfer",
            TransferQueueOperationKind.File => "QueueOperationFile",
            TransferQueueOperationKind.Batch => "QueueOperationBatch",
            TransferQueueOperationKind.Text => "QueueOperationText",
            TransferQueueOperationKind.Receive => "QueueOperationReceive",
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unsupported queue operation.")
        };
        return AppText.Get(key);
    }

    public static string NearbyCount(int count)
        => count == 1
            ? AppText.Get("NearbyDeviceCountOne")
            : AppText.Format("NearbyDeviceCountFormat", count);

    public static string LastSeen(DateTimeOffset? timestamp)
        => timestamp is null
            ? AppText.Get("LastSeenUnknown")
            : AppText.Format("LastSeenAtFormat", timestamp.Value.LocalDateTime);

    public static string TrustedCount(int count)
        => count == 1
            ? AppText.Get("TrustedDeviceCountOne")
            : AppText.Format("TrustedDeviceCountFormat", count);

    public static string TrustedAt(DateTimeOffset trustedUtc, DateTimeOffset lastSeenUtc)
        => AppText.Format("TrustedAtFormat", trustedUtc.LocalDateTime, lastSeenUtc.LocalDateTime);
}
