using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Transfer;

public static class BatchManifestValidator
{
    public static IReadOnlyList<FileManifestEntry> Validate(
        IReadOnlyList<FileManifestEntry>? files,
        long? declaredTotalBytes)
    {
        if (files is null || files.Count == 0 || files.Count > ProtocolConstants.MaxBatchFiles)
            throw new InvalidDataException("Invalid batch file count.");

        var validated = new FileManifestEntry[files.Count];
        var portableDestinationKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long actualTotal = 0;
        for (var i = 0; i < files.Count; i++)
        {
            var entry = ManifestValidator.ValidateEntry(files[i]);
            var destinationKey = FileNameSanitizer.GetPortableCollisionKey(entry.RelativePath);
            if (!portableDestinationKeys.Add(destinationKey))
                throw new InvalidDataException("Batch contains paths that collide after safe filename normalization.");

            actualTotal = checked(actualTotal + entry.Length);
            if (actualTotal > ProtocolConstants.MaxBatchBytes)
                throw new InvalidDataException("Batch exceeds the SwiftDrop total-size safety limit.");
            validated[i] = entry;
        }

        if (declaredTotalBytes is null || declaredTotalBytes.Value != actualTotal)
            throw new InvalidDataException("Batch total size mismatch.");

        return validated;
    }
}
