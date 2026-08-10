#if IOS || MACCATALYST
using Foundation;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;

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
        string? destination = null;
        try
        {
            accessGranted = url.StartAccessingSecurityScopedResource();
            var source = new FileInfo(sourcePath);
            if (!source.Exists || source.Length < 0 || source.Length > ProtocolConstants.MaxSingleFileBytes)
                return false;

            var safeName = FileNameSanitizer.SanitizeSegment(source.Name);
            var stagingRoot = Path.Combine(FileSystem.CacheDirectory, "shared-input");
            Directory.CreateDirectory(stagingRoot);
            destination = Path.Combine(stagingRoot, $"{Guid.NewGuid():N}-{safeName}");

            await using var input = new FileStream(
                source.FullName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                128 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            if (input.Length != source.Length) return false;

            await using var output = new FileStream(
                destination,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                128 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            var remaining = source.Length;
            var buffer = new byte[128 * 1024];
            while (remaining > 0)
            {
                ct.ThrowIfCancellationRequested();
                var requested = (int)Math.Min(buffer.Length, remaining);
                var read = await input.ReadAsync(buffer.AsMemory(0, requested), ct);
                if (read == 0) throw new EndOfStreamException("Shared source ended before its declared length.");
                await output.WriteAsync(buffer.AsMemory(0, read), ct);
                remaining -= read;
            }
            await output.FlushAsync(ct);

            if (input.Length != source.Length)
            {
                TryDelete(destination);
                return false;
            }

            ExternalInputInbox.AddSharedFile(destination);
            destination = null;
            return true;
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(destination)) TryDelete(destination);
            return false;
        }
        finally
        {
            if (accessGranted) url.StopAccessingSecurityScopedResource();
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
        }
    }
}
#endif
