#if IOS || MACCATALYST
using Foundation;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.App.Services;

public static class AppleExternalFileStager
{
    public static async Task<bool> TryStageAsync(NSUrl url, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(url);
        if (!url.IsFileUrl) return false;

        var sourcePath = url.Path;
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath)) return false;

        var accessGranted = false;
        try
        {
            accessGranted = url.StartAccessingSecurityScopedResource();
            var stagingRoot = Path.Combine(FileSystem.CacheDirectory, "shared-input");
            var staged = await ExternalFileStager.StageFileAsync(
                sourcePath,
                stagingRoot,
                ProtocolConstants.MaxSingleFileBytes,
                ct);
            ExternalInputInbox.AddSharedFile(staged);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (accessGranted) url.StopAccessingSecurityScopedResource();
        }
    }
}
#endif
