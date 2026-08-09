using SwiftDrop.Core.Models;

namespace SwiftDrop.Core.Transfer;

public static class BatchTransferPlanValidator
{
    public static IReadOnlyDictionary<string, BatchItemPlan> Validate(
        IReadOnlyList<FileManifestEntry> sourceEntries,
        BatchTransferResponse response)
    {
        ArgumentNullException.ThrowIfNull(sourceEntries);
        ArgumentNullException.ThrowIfNull(response);
        if (sourceEntries.Count == 0)
            throw new InvalidDataException("Batch source manifest is empty.");
        if (response.Items is null)
            throw new InvalidDataException("Receiver batch plan is missing.");

        var sourceByPath = sourceEntries.ToDictionary(x => x.RelativePath, StringComparer.Ordinal);
        if (sourceByPath.Count != sourceEntries.Count)
            throw new InvalidDataException("Batch source manifest contains duplicate paths.");
        if (response.Items.Count > sourceEntries.Count)
            throw new InvalidDataException("Receiver returned too many batch plan items.");

        var plans = new Dictionary<string, BatchItemPlan>(StringComparer.Ordinal);
        foreach (var plan in response.Items)
        {
            if (plan is null || string.IsNullOrWhiteSpace(plan.RelativePath))
                throw new InvalidDataException("Receiver returned an invalid batch item.");
            if (!sourceByPath.TryGetValue(plan.RelativePath, out var source))
                throw new InvalidDataException("Receiver returned an unknown batch item.");
            if (!plans.TryAdd(plan.RelativePath, plan))
                throw new InvalidDataException("Receiver returned a duplicate batch item.");
            if (plan.ResumeOffset < 0 || plan.ResumeOffset > source.Length)
                throw new InvalidDataException("Receiver returned an invalid resume offset.");
            if (!plan.Accepted && plan.ResumeOffset != 0)
                throw new InvalidDataException("Rejected batch items cannot advertise a resume offset.");
        }

        if (!response.Accepted)
        {
            if (plans.Values.Any(x => x.Accepted))
                throw new InvalidDataException("Rejected batch response contains accepted items.");
            return plans;
        }

        if (plans.Count != sourceEntries.Count)
            throw new InvalidDataException("Accepted batch response omitted one or more source items.");
        if (!plans.Values.Any(x => x.Accepted))
            throw new InvalidDataException("Accepted batch response did not accept any files.");

        return plans;
    }
}
