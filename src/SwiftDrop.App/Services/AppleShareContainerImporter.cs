#if IOS || MACCATALYST
using System.Text.Json;
using System.Text.Json.Serialization;
using Foundation;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.App.Services;

public static class AppleShareContainerImporter
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static async Task<int> ImportPendingAsync(CancellationToken ct = default)
    {
        await Gate.WaitAsync(ct);
        try
        {
            var containerUrl = NSFileManager.DefaultManager.GetContainerUrl(ExternalSharePackageConstants.AppleAppGroupId);
            var containerPath = containerUrl?.Path;
            if (string.IsNullOrWhiteSpace(containerPath)) return 0;

            var inboxRoot = Path.Combine(containerPath, ExternalSharePackageConstants.InboxDirectoryName);
            if (!Directory.Exists(inboxRoot)) return 0;

            foreach (var packagePath in Directory.EnumerateDirectories(inboxRoot, "pending-*", SearchOption.TopDirectoryOnly)
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                ct.ThrowIfCancellationRequested();
                var outcome = await TryImportPackageAsync(inboxRoot, packagePath, ct);
                if (outcome == ImportOutcome.Imported) return 1;
            }

            return 0;
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task<ImportOutcome> TryImportPackageAsync(
        string inboxRoot,
        string packagePath,
        CancellationToken ct)
    {
        string? stagingRoot = null;
        try
        {
            var safePackagePath = Path.GetFullPath(packagePath);
            var expectedPrefix = Path.GetFullPath(inboxRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!safePackagePath.StartsWith(expectedPrefix, PathComparisonPolicy.Comparison))
                throw new InvalidDataException("External share package escaped the App Group inbox.");

            var directoryName = Path.GetFileName(safePackagePath);
            if (!directoryName.StartsWith("pending-", StringComparison.Ordinal))
                throw new InvalidDataException("External share package directory has an invalid name.");
            var directoryPackageId = directoryName["pending-".Length..];
            if (!ExternalSharePackageValidator.IsPackageId(directoryPackageId))
                throw new InvalidDataException("External share package directory identifier is invalid.");

            var manifestPath = Path.Combine(safePackagePath, "manifest.json");
            var manifestInfo = new FileInfo(manifestPath);
            if (!manifestInfo.Exists || manifestInfo.Length is <= 0 or > ProtocolConstants.HeaderLimitBytes)
                throw new InvalidDataException("External share package manifest is missing or oversized.");

            var manifestBytes = await File.ReadAllBytesAsync(manifestPath, ct);
            StrictJsonGuard.Validate(manifestBytes, maxDepth: 16);
            var manifest = JsonSerializer.Deserialize<ExternalSharePackageManifest>(manifestBytes, JsonOptions)
                           ?? throw new InvalidDataException("External share package manifest is empty.");
            ExternalSharePackageValidator.Validate(manifest, DateTimeOffset.UtcNow);
            if (!string.Equals(manifest.PackageId, directoryPackageId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("External share package directory and manifest identifiers differ.");

            var sourceFilesRoot = Path.Combine(safePackagePath, "files");
            var validatedSources = new List<(ExternalSharePackageFile Item, string SourcePath)>(manifest.Files.Count);
            foreach (var item in manifest.Files)
            {
                var sourcePath = PathGuard.ResolveUnderRoot(sourceFilesRoot, item.FileName);
                var source = new FileInfo(sourcePath);
                if (!source.Exists || source.Length != item.Length)
                    throw new InvalidDataException("Shared package file does not match its manifest.");
                validatedSources.Add((item, source.FullName));
            }

            stagingRoot = Path.Combine(FileSystem.CacheDirectory, "shared-input", manifest.PackageId);
            if (Directory.Exists(stagingRoot)) Directory.Delete(stagingRoot, recursive: true);
            Directory.CreateDirectory(stagingRoot);

            var stagedPaths = new List<string>(validatedSources.Count);
            foreach (var source in validatedSources)
            {
                ct.ThrowIfCancellationRequested();
                var temporaryPath = await ExternalFileStager.StageFileAsync(
                    source.SourcePath,
                    stagingRoot,
                    source.Item.Length,
                    ct);
                var finalPath = Path.Combine(stagingRoot, source.Item.FileName);
                File.Move(temporaryPath, finalPath, overwrite: false);
                stagedPaths.Add(finalPath);
            }

            ExternalInputInbox.AddSharedBatch(manifest.Text, stagedPaths);
            DeleteBestEffort(safePackagePath);
            return ImportOutcome.Imported;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            if (!string.IsNullOrWhiteSpace(stagingRoot)) DeleteBestEffort(stagingRoot);
            throw;
        }
        catch (Exception ex) when (ex is InvalidDataException or JsonException or UnauthorizedAccessException or FileNotFoundException)
        {
            if (!string.IsNullOrWhiteSpace(stagingRoot)) DeleteBestEffort(stagingRoot);
            DeleteBestEffort(packagePath);
            return ImportOutcome.DiscardedInvalid;
        }
        catch (IOException)
        {
            if (!string.IsNullOrWhiteSpace(stagingRoot)) DeleteBestEffort(stagingRoot);
            return ImportOutcome.RetryLater;
        }
    }

    private static void DeleteBestEffort(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
            else if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
        }
    }

    private enum ImportOutcome
    {
        Imported,
        DiscardedInvalid,
        RetryLater
    }
}
#endif
