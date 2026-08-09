using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;

namespace SwiftDrop.Core.Transfer;

public static class ManifestValidator
{
    public static FileManifestEntry ValidateEntry(FileManifestEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (string.IsNullOrWhiteSpace(entry.RelativePath) || entry.RelativePath.Length > 1024)
            throw new InvalidDataException("Invalid transfer path metadata.");
        if (entry.RelativePath.Any(char.IsControl))
            throw new InvalidDataException("Transfer path contains control characters.");
        if (entry.Length < 0 || entry.Length > ProtocolConstants.MaxSingleFileBytes)
            throw new InvalidDataException("Unsafe file size.");
        if (!IsSha256(entry.Sha256))
            throw new InvalidDataException("Invalid SHA-256 metadata.");
        if (entry.LastWriteUtc < DateTimeOffset.UnixEpoch || entry.LastWriteUtc > DateTimeOffset.UtcNow.AddDays(2))
            throw new InvalidDataException("Invalid file timestamp metadata.");
        return entry;
    }

    public static bool IsSha256(string? value)
    {
        if (value is null || value.Length != 64) return false;
        foreach (var c in value)
        {
            var hex = c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';
            if (!hex) return false;
        }
        return true;
    }
}
