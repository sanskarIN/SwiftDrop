#if MACCATALYST
using Foundation;
using SwiftDrop.App.Services;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;
using SwiftDrop.Core.Storage;
using UIKit;

namespace SwiftDrop.App;

public partial class MainPage
{
    private UIView? _macDropHostView;
    private UIDropInteraction? _macDropInteraction;
    private MacDropDelegate? _macDropDelegate;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        DetachMacDropInteraction();

        var hostView = Handler?.PlatformView switch
        {
            UIView view => view,
            UIViewController controller => controller.View,
            _ => null
        };
        if (hostView is null) return;

        _macDropDelegate = new MacDropDelegate(this);
        _macDropInteraction = new UIDropInteraction(_macDropDelegate);
        _macDropHostView = hostView;
        hostView.AddInteraction(_macDropInteraction);
    }

    partial void DisposePlatformIntegrations() => DetachMacDropInteraction();

    private void DetachMacDropInteraction()
    {
        if (_macDropHostView is not null && _macDropInteraction is not null)
        {
            try { _macDropHostView.RemoveInteraction(_macDropInteraction); } catch { }
        }
        _macDropInteraction?.Dispose();
        _macDropInteraction = null;
        _macDropDelegate?.Dispose();
        _macDropDelegate = null;
        _macDropHostView = null;
    }

    private async Task ProcessMacDropAsync(IReadOnlyList<UIDragItem> items)
    {
        if (items.Count == 0) return;
        var root = Path.Combine(FileSystem.CacheDirectory, "shared-input", "drop-" + Guid.NewGuid().ToString("N"));
        var budget = new DropBudget();
        var staged = new List<string>();
        var texts = new List<string>();

        try
        {
            Directory.CreateDirectory(root);
            foreach (var dragItem in items.Take(ExternalSharePackageConstants.MaximumItems))
            {
                var provider = dragItem.ItemProvider;
                var path = await TryStageDroppedProviderAsync(provider, root, budget);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    staged.Add(path);
                    continue;
                }

                var text = await TryLoadDroppedTextAsync(provider);
                if (!string.IsNullOrWhiteSpace(text)) texts.Add(text);
            }

            var combinedText = string.Join(Environment.NewLine, texts).Trim();
            string? pairingLink = null;
            if (combinedText.StartsWith("swiftdrop://pair", StringComparison.OrdinalIgnoreCase) && combinedText.Length <= 16_384)
            {
                pairingLink = combinedText;
                combinedText = string.Empty;
            }

            if (staged.Count == 0 && string.IsNullOrWhiteSpace(combinedText) && pairingLink is null)
            {
                DeleteBestEffort(root);
                return;
            }

            if (pairingLink is not null) ExternalInputInbox.SetPairingLink(pairingLink);
            ExternalInputInbox.AddSharedBatch(string.IsNullOrWhiteSpace(combinedText) ? null : combinedText, staged);
        }
        catch
        {
            DeleteBestEffort(root);
        }
    }

    private static async Task<string?> TryStageDroppedProviderAsync(
        NSItemProvider provider,
        string stagingRoot,
        DropBudget budget)
    {
        if (provider.HasItemConformingTo("public.file-url"))
        {
            var direct = await LoadDroppedFileUrlAsync(provider, stagingRoot, budget);
            if (direct is not null) return direct;
        }

        var identifiers = provider.RegisteredTypeIdentifiers ?? Array.Empty<string>();
        var typeIdentifier = identifiers.FirstOrDefault(identifier => !IsTextOrUrlType(identifier));
        if (string.IsNullOrWhiteSpace(typeIdentifier)) return null;
        return await LoadDroppedFileRepresentationAsync(provider, typeIdentifier, stagingRoot, budget);
    }

    private static Task<string?> LoadDroppedFileUrlAsync(
        NSItemProvider provider,
        string stagingRoot,
        DropBudget budget)
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        provider.LoadItem("public.file-url", null, (item, error) =>
        {
            if (error is not null || item is not NSUrl url || !url.IsFileUrl)
            {
                tcs.TrySetResult(null);
                return;
            }

            try { tcs.TrySetResult(CopyDroppedPath(url, provider.SuggestedName, stagingRoot, budget)); }
            catch (Exception ex) { tcs.TrySetException(ex); }
        });
        return tcs.Task;
    }

    private static Task<string?> LoadDroppedFileRepresentationAsync(
        NSItemProvider provider,
        string typeIdentifier,
        string stagingRoot,
        DropBudget budget)
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        provider.LoadFileRepresentation(typeIdentifier, (url, error) =>
        {
            if (error is not null || url is null || !url.IsFileUrl)
            {
                tcs.TrySetResult(null);
                return;
            }

            try { tcs.TrySetResult(CopyDroppedPath(url, provider.SuggestedName, stagingRoot, budget)); }
            catch (Exception ex) { tcs.TrySetException(ex); }
        });
        return tcs.Task;
    }

    private static string CopyDroppedPath(
        NSUrl url,
        string? suggestedName,
        string stagingRoot,
        DropBudget budget)
    {
        var granted = false;
        try
        {
            granted = url.StartAccessingSecurityScopedResource();
            var sourcePath = url.Path;
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new InvalidDataException("Dropped source path is unavailable.");

            if (File.Exists(sourcePath))
            {
                var requestedName = string.IsNullOrWhiteSpace(suggestedName)
                    ? Path.GetFileName(sourcePath)
                    : suggestedName;
                var destination = PathGuard.GetCollisionFreePath(
                    Path.Combine(stagingRoot, FileNameSanitizer.SanitizeSegment(requestedName)));
                CopyDroppedFile(sourcePath, destination, budget);
                return destination;
            }

            if (Directory.Exists(sourcePath))
            {
                var requestedName = string.IsNullOrWhiteSpace(suggestedName)
                    ? new DirectoryInfo(sourcePath).Name
                    : suggestedName;
                var destination = PathGuard.GetCollisionFreePath(
                    Path.Combine(stagingRoot, FileNameSanitizer.SanitizeSegment(requestedName)));
                CopyDroppedDirectory(sourcePath, destination, budget);
                return destination;
            }

            throw new FileNotFoundException("Dropped source is unavailable.", sourcePath);
        }
        finally
        {
            if (granted) url.StopAccessingSecurityScopedResource();
        }
    }

    private static void CopyDroppedDirectory(string sourceRoot, string destinationRoot, DropBudget budget)
    {
        var source = new DirectoryInfo(sourceRoot);
        if ((source.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Dropped symbolic-link directories are not accepted.");

        Directory.CreateDirectory(destinationRoot);
        var stack = new Stack<(DirectoryInfo Source, string Destination)>();
        stack.Push((source, destinationRoot));
        var directoryCount = 0;

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (++directoryCount > ProtocolConstants.MaxBatchFiles * 2)
                throw new InvalidDataException("Dropped folder contains too many directories.");

            foreach (var entry in current.Source.EnumerateFileSystemInfos())
            {
                if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Dropped symbolic links are not accepted.");

                if (entry is DirectoryInfo directory)
                {
                    var child = Path.Combine(current.Destination, FileNameSanitizer.SanitizeSegment(directory.Name));
                    Directory.CreateDirectory(child);
                    stack.Push((directory, child));
                }
                else if (entry is FileInfo file)
                {
                    var child = Path.Combine(current.Destination, FileNameSanitizer.SanitizeSegment(file.Name));
                    child = PathGuard.GetCollisionFreePath(child);
                    CopyDroppedFile(file.FullName, child, budget);
                }
            }
        }
    }

    private static void CopyDroppedFile(string sourcePath, string destinationPath, DropBudget budget)
    {
        var source = new FileInfo(sourcePath);
        if (!source.Exists) throw new FileNotFoundException("Dropped file is unavailable.", sourcePath);
        if ((source.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Dropped symbolic-link files are not accepted.");
        budget.Reserve(source.Length);
        StorageCapacityGuard.EnsureCapacity(destinationPath, source.Length);

        using var input = new FileStream(source.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, FileOptions.SequentialScan);
        if (input.Length != source.Length) throw new IOException("Dropped file changed before staging.");
        using var output = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, FileOptions.SequentialScan);
        var remaining = source.Length;
        var buffer = new byte[128 * 1024];
        while (remaining > 0)
        {
            var requested = (int)Math.Min(buffer.Length, remaining);
            var read = input.Read(buffer, 0, requested);
            if (read == 0) throw new EndOfStreamException("Dropped file ended unexpectedly.");
            output.Write(buffer, 0, read);
            remaining -= read;
        }
        output.Flush(flushToDisk: true);
        if (new FileInfo(source.FullName).Length != source.Length || new FileInfo(destinationPath).Length != source.Length)
            throw new IOException("Dropped file changed while staging.");
    }

    private static Task<string?> TryLoadDroppedTextAsync(NSItemProvider provider)
    {
        var typeIdentifier = provider.HasItemConformingTo("public.plain-text")
            ? "public.plain-text"
            : provider.HasItemConformingTo("public.text")
                ? "public.text"
                : provider.HasItemConformingTo("public.url")
                    ? "public.url"
                    : null;
        if (typeIdentifier is null) return Task.FromResult<string?>(null);

        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        provider.LoadItem(typeIdentifier, null, (item, error) =>
        {
            if (error is not null)
            {
                tcs.TrySetResult(null);
                return;
            }
            tcs.TrySetResult(item switch
            {
                NSString text => text.ToString(),
                NSUrl url when !url.IsFileUrl => url.AbsoluteString,
                _ => null
            });
        });
        return tcs.Task;
    }

    private static bool IsTextOrUrlType(string identifier)
        => identifier is "public.text" or "public.plain-text" or "public.utf8-plain-text" or "public.url" or "public.file-url";

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

    private sealed class DropBudget
    {
        private int _files;
        private long _bytes;

        public void Reserve(long length)
        {
            if (length < 0 || length > ProtocolConstants.MaxSingleFileBytes)
                throw new InvalidDataException("Dropped file exceeds SwiftDrop limits.");
            if (++_files > ProtocolConstants.MaxBatchFiles)
                throw new InvalidDataException("Dropped content contains too many files.");
            _bytes = checked(_bytes + length);
            if (_bytes > ProtocolConstants.MaxBatchBytes)
                throw new InvalidDataException("Dropped content exceeds the aggregate SwiftDrop limit.");
        }
    }

    private sealed class MacDropDelegate : UIDropInteractionDelegate
    {
        private readonly WeakReference<MainPage> _owner;

        public MacDropDelegate(MainPage owner) => _owner = new WeakReference<MainPage>(owner);

        public override UIDropProposal SessionDidUpdate(UIDropInteraction interaction, IUIDropSession session)
            => new(UIDropOperation.Copy);

        public override void PerformDrop(UIDropInteraction interaction, IUIDropSession session)
        {
            if (!_owner.TryGetTarget(out var owner)) return;
            _ = owner.ProcessMacDropAsync(session.Items);
        }
    }
}
#endif
