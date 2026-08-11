using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Transfer;

public static class ExternalSharePackageFileSetValidator
{
    private static readonly StringComparer PackageNameComparer = StringComparer.OrdinalIgnoreCase;

    public static void ValidateExact(
        IReadOnlyList<ExternalSharePackageFile> declaredFiles,
        IEnumerable<string> actualFileNames)
    {
        ArgumentNullException.ThrowIfNull(declaredFiles);
        ArgumentNullException.ThrowIfNull(actualFileNames);

        var declared = new HashSet<string>(PackageNameComparer);
        foreach (var item in declaredFiles)
        {
            ArgumentNullException.ThrowIfNull(item);
            var name = ValidateSingleSegment(item.FileName, nameof(declaredFiles));
            if (!declared.Add(name))
                throw new InvalidDataException("External share package declares duplicate portable filenames.");
        }

        var actual = new HashSet<string>(PackageNameComparer);
        foreach (var value in actualFileNames)
        {
            var name = ValidateSingleSegment(value, nameof(actualFileNames));
            if (!actual.Add(name))
                throw new InvalidDataException("External share package contains duplicate portable filenames.");
        }

        if (declared.Count != actual.Count || !declared.SetEquals(actual))
            throw new InvalidDataException("External share package file set does not exactly match its manifest.");
    }

    private static string ValidateSingleSegment(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("External share package filename is required.", parameterName);

        if (!string.Equals(value, Path.GetFileName(value), StringComparison.Ordinal) ||
            value.Contains('/') || value.Contains('\\'))
            throw new InvalidDataException("External share package filenames must be single path segments.");

        var sanitized = FileNameSanitizer.SanitizeSegment(value);
        if (!string.Equals(sanitized, value, StringComparison.Ordinal))
            throw new InvalidDataException("External share package filename is not canonical.");
        return value;
    }
}
