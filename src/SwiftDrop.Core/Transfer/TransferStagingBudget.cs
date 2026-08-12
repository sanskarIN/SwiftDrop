namespace SwiftDrop.Core.Transfer;

public sealed class TransferStagingBudget
{
    private readonly int _maximumFiles;
    private readonly long _maximumAggregateBytes;
    private readonly long _maximumSingleFileBytes;
    private int _committedFiles;
    private long _committedBytes;

    public TransferStagingBudget(
        int maximumFiles,
        long maximumAggregateBytes,
        long maximumSingleFileBytes)
    {
        if (maximumFiles <= 0) throw new ArgumentOutOfRangeException(nameof(maximumFiles));
        if (maximumAggregateBytes < 0) throw new ArgumentOutOfRangeException(nameof(maximumAggregateBytes));
        if (maximumSingleFileBytes < 0) throw new ArgumentOutOfRangeException(nameof(maximumSingleFileBytes));
        if (maximumSingleFileBytes > maximumAggregateBytes)
            throw new ArgumentException("Single-file limit cannot exceed aggregate limit.", nameof(maximumSingleFileBytes));

        _maximumFiles = maximumFiles;
        _maximumAggregateBytes = maximumAggregateBytes;
        _maximumSingleFileBytes = maximumSingleFileBytes;
    }

    public int CommittedFiles => _committedFiles;
    public long CommittedBytes => _committedBytes;
    public int RemainingFiles => Math.Max(0, _maximumFiles - _committedFiles);
    public long RemainingAggregateBytes => Math.Max(0, _maximumAggregateBytes - _committedBytes);
    public long MaximumBytesForNextFile => RemainingFiles == 0
        ? 0
        : Math.Min(_maximumSingleFileBytes, RemainingAggregateBytes);

    public void EnsureCanStage(long length)
    {
        if (length < 0 || length > _maximumSingleFileBytes)
            throw new InvalidDataException("Staged file exceeds the per-file limit.");
        if (_committedFiles >= _maximumFiles)
            throw new InvalidDataException("Staged content contains too many files.");
        if (length > RemainingAggregateBytes)
            throw new InvalidDataException("Staged content exceeds the aggregate byte limit.");
    }

    public void Commit(long length)
    {
        EnsureCanStage(length);
        _committedFiles = checked(_committedFiles + 1);
        _committedBytes = checked(_committedBytes + length);
    }
}
