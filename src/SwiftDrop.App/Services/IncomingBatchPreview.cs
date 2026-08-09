using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;

namespace SwiftDrop.App.Services;

public sealed record IncomingBatchPreview(
    string SenderDeviceId,
    string SenderDeviceName,
    string SenderCertificateFingerprint,
    string TransferId,
    IReadOnlyList<FileManifestEntry> Files)
{
    public int FileCount => Files.Count;
    public long TotalBytes => Files.Sum(x => x.Length);
    public FileRiskLevel HighestRisk => Files.Select(x => FileRiskClassifier.Classify(x.RelativePath)).DefaultIfEmpty(FileRiskLevel.Normal).Max();
}

public sealed record IncomingBatchDecision(
    bool Accepted,
    IReadOnlySet<string> AcceptedRelativePaths)
{
    public static IncomingBatchDecision Reject { get; } = new(false, new HashSet<string>(StringComparer.Ordinal));

    public static IncomingBatchDecision AcceptAll(IEnumerable<FileManifestEntry> files)
        => new(true, files.Select(x => x.RelativePath).ToHashSet(StringComparer.Ordinal));
}
