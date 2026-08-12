using System.Text.Json;
using Foundation;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.ShareExtension;

internal sealed record AppleSharedFileSource(NSUrl Url, string? SuggestedName);

internal static class AppleSharePackageWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    public static async Task<string> WriteAsync(
        IReadOnlyList<AppleSharedFileSource> sources,
        string? text,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sources);
        if (sources.Count > ExternalSharePackageConstants.MaximumItems)
            throw new InvalidDataException("Too many shared items.");

        var packageId = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;
        var containerUrl = NSFileManager.DefaultManager.GetContainerUrl(ExternalSharePackageConstants.AppleAppGroupId)
                           ?? throw new IOException("SwiftDrop App Group container is unavailable.");
        var containerPath = containerUrl.Path;
        if (string.IsNullOrWhiteSpace(containerPath))
            throw new IOException("SwiftDrop App Group container path is unavailable.");

        var inboxRoot = Path.Combine(containerPath, ExternalSharePackageConstants.InboxDirectoryName);
        Directory.CreateDirectory(inboxRoot);
        var stagingRoot = Path.Combine(inboxRoot, $".staging-{packageId}");
        var pendingRoot = Path.Combine(inboxRoot, $"pending-{packageId}");
        var filesRoot = Path.Combine(stagingRoot, "files");
        var access = new List<(NSUrl Url, bool Granted)>(sources.Count);

        try
        {
            Directory.CreateDirectory(filesRoot);
            var prepared = new List<(AppleSharedFileSource Source, ExternalSharePackageFile Item)>(sources.Count);
            var collisionKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var source in sources)
            {
                ct.ThrowIfCancellationRequested();
                ArgumentNullException.ThrowIfNull(source);
                ArgumentNullException.ThrowIfNull(source.Url);
                if (!source.Url.IsFileUrl)
                    throw new InvalidDataException("Shared attachment is not a file URL.");

                var granted = source.Url.StartAccessingSecurityScopedResource();
                access.Add((source.Url, granted));

                var path = source.Url.Path;
                if (string.IsNullOrWhiteSpace(path))
                    throw new InvalidDataException("Shared attachment path is unavailable.");
                var info = new FileInfo(path);
                if (!info.Exists)
                    throw new FileNotFoundException("Shared attachment is unavailable.", path);

                var requestedName = string.IsNullOrWhiteSpace(source.SuggestedName)
                    ? info.Name
                    : source.SuggestedName;
                var safeName = MakeUniqueName(FileNameSanitizer.SanitizeSegment(requestedName), collisionKeys);
                prepared.Add((source, new ExternalSharePackageFile(safeName, info.Length)));
            }

            var manifest = new ExternalSharePackageManifest(
                ExternalSharePackageConstants.CurrentVersion,
                packageId,
                now.ToUnixTimeSeconds(),
                text,
                prepared.Select(item => item.Item).ToArray());
            ExternalSharePackageValidator.Validate(manifest, now);

            foreach (var item in prepared)
            {
                ct.ThrowIfCancellationRequested();
                var sourcePath = item.Source.Url.Path
                                 ?? throw new InvalidDataException("Shared attachment path disappeared.");
                var temporaryPath = await ExternalFileStager.StageFileAsync(
                    sourcePath,
                    filesRoot,
                    item.Item.Length,
                    ct);
                var finalPath = Path.Combine(filesRoot, item.Item.FileName);
                File.Move(temporaryPath, finalPath, overwrite: false);
                if (new FileInfo(finalPath).Length != item.Item.Length)
                    throw new IOException("Shared attachment changed while staging.");
            }

            var manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest, JsonOptions);
            if (manifestBytes.Length is <= 0 or > ProtocolConstants.HeaderLimitBytes)
                throw new InvalidDataException("External share package manifest exceeds the metadata limit.");
            await File.WriteAllBytesAsync(Path.Combine(stagingRoot, "manifest.json"), manifestBytes, ct);

            if (Directory.Exists(pendingRoot))
                throw new IOException("External share package identifier collision.");
            Directory.Move(stagingRoot, pendingRoot);
            return packageId;
        }
        catch
        {
            DeleteBestEffort(stagingRoot);
            DeleteBestEffort(pendingRoot);
            throw;
        }
        finally
        {
            foreach (var item in access)
            {
                if (item.Granted) item.Url.StopAccessingSecurityScopedResource();
            }
        }
    }

    private static string MakeUniqueName(string safeName, HashSet<string> collisionKeys)
    {
        var key = FileNameSanitizer.GetPortableCollisionKey(safeName);
        if (collisionKeys.Add(key)) return safeName;

        for (var index = 1; index < 10_000; index++)
        {
            var candidate = FileNameSanitizer.CreateCollisionSegment(safeName, index);
            key = FileNameSanitizer.GetPortableCollisionKey(candidate);
            if (collisionKeys.Add(key)) return candidate;
        }
        throw new IOException("Unable to deconflict shared attachment names.");
    }

    private static void DeleteBestEffort(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
        catch
        {
        }
    }
}
