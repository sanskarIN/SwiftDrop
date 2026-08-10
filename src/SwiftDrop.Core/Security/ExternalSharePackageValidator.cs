using System.Text;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;

namespace SwiftDrop.Core.Security;

public static class ExternalSharePackageValidator
{
    public static ExternalSharePackageManifest Validate(
        ExternalSharePackageManifest manifest,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (!string.Equals(manifest.Version, ExternalSharePackageConstants.CurrentVersion, StringComparison.Ordinal))
            throw new InvalidDataException("Unsupported external share package version.");
        if (!IsPackageId(manifest.PackageId))
            throw new InvalidDataException("Invalid external share package identifier.");

        DateTimeOffset createdUtc;
        try
        {
            createdUtc = DateTimeOffset.FromUnixTimeSeconds(manifest.CreatedUnixSeconds);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new InvalidDataException("Invalid external share package timestamp.", ex);
        }

        if (createdUtc > nowUtc + ExternalSharePackageConstants.MaximumFutureClockSkew)
            throw new InvalidDataException("External share package timestamp is too far in the future.");
        if (createdUtc < nowUtc - ExternalSharePackageConstants.MaximumPackageAge)
            throw new InvalidDataException("External share package has expired.");

        var text = manifest.Text;
        if (text is not null && Encoding.UTF8.GetByteCount(text) > ProtocolConstants.MaxTextSnippetBytes)
            throw new InvalidDataException("Shared text exceeds the SwiftDrop text limit.");

        var files = manifest.Files ?? throw new InvalidDataException("External share package file list is required.");
        if (files.Count > ExternalSharePackageConstants.MaximumItems)
            throw new InvalidDataException("External share package contains too many files.");
        if (string.IsNullOrEmpty(text) && files.Count == 0)
            throw new InvalidDataException("External share package has no usable content.");

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long totalBytes = 0;
        foreach (var file in files)
        {
            if (file is null) throw new InvalidDataException("External share package contains an invalid file item.");
            if (string.IsNullOrWhiteSpace(file.FileName))
                throw new InvalidDataException("Shared file name is required.");

            var safeName = FileNameSanitizer.SanitizeSegment(file.FileName);
            if (!string.Equals(safeName, file.FileName, StringComparison.Ordinal))
                throw new InvalidDataException("Shared file name is not in canonical safe form.");
            if (file.Length < 0 || file.Length > ProtocolConstants.MaxSingleFileBytes)
                throw new InvalidDataException("Shared file length exceeds protocol limits.");

            var key = FileNameSanitizer.GetPortableCollisionKey(file.FileName);
            if (!keys.Add(key))
                throw new InvalidDataException("External share package contains colliding file names.");
            totalBytes = checked(totalBytes + file.Length);
            if (totalBytes > ProtocolConstants.MaxBatchBytes)
                throw new InvalidDataException("External share package exceeds the aggregate batch limit.");
        }

        return manifest;
    }

    public static bool IsPackageId(string? value)
        => value is { Length: 32 } && value.All(Uri.IsHexDigit);
}
