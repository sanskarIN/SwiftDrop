using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;

namespace SwiftDrop.Desktop.Services;

public sealed record DesktopIncomingTransferPreview(
    string SenderDeviceId,
    string SenderDeviceName,
    string SenderCertificateFingerprint,
    FileManifestEntry Entry,
    FileRiskLevel RiskLevel);

public sealed record DesktopIncomingBatchPreview(
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

public sealed record DesktopIncomingBatchDecision(
    bool Accepted,
    IReadOnlySet<string> AcceptedRelativePaths)
{
    public static DesktopIncomingBatchDecision Reject { get; } = new(false, new HashSet<string>(StringComparer.Ordinal));

    public static DesktopIncomingBatchDecision AcceptAll(IEnumerable<FileManifestEntry> files)
        => new(true, files.Select(x => x.RelativePath).ToHashSet(StringComparer.Ordinal));
}

public enum DesktopIncomingTextDecision
{
    Reject,
    Accept,
    AcceptAndCopy
}

public sealed record DesktopIncomingTextPreview(
    string SenderDeviceId,
    string SenderDeviceName,
    string SenderCertificateFingerprint,
    string Text,
    DateTimeOffset ExpiresUtc)
{
    public int CharacterCount => Text.Length;
}

public sealed record DesktopIncomingPairingRequest(
    string SenderDeviceId,
    string SenderDeviceName,
    string SenderCertificateFingerprint);
