namespace SwiftDrop.Core.Models;

public sealed record ExternalSharePackageManifest(
    string Version,
    string PackageId,
    long CreatedUnixSeconds,
    string? Text,
    IReadOnlyList<ExternalSharePackageFile> Files);

public sealed record ExternalSharePackageFile(
    string FileName,
    long Length);
