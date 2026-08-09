using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;

namespace SwiftDrop.Core.Transfer;

public static class BatchCapacityPlanner
{
    public static long CalculateRemainingBytes(
        IReadOnlyList<FileManifestEntry> entries,
        IReadOnlyDictionary<string, long> resumeOffsets)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(resumeOffsets);
        if (entries.Count == 0 || entries.Count > ProtocolConstants.MaxBatchFiles)
            throw new InvalidDataException("Invalid batch entry count.");

        long remaining = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            ManifestValidator.ValidateEntry(entry);
            if (!seen.Add(entry.RelativePath))
                throw new InvalidDataException("Duplicate batch path.");
            var offset = resumeOffsets.TryGetValue(entry.RelativePath, out var value) ? value : 0;
            if (offset < 0 || offset > entry.Length)
                throw new InvalidDataException("Invalid batch resume offset.");
            remaining = checked(remaining + (entry.Length - offset));
            if (remaining > ProtocolConstants.MaxBatchBytes)
                throw new InvalidDataException("Remaining batch payload exceeds the aggregate safety limit.");
        }
        return remaining;
    }
}
