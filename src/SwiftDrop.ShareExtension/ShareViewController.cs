using System.Text;
using Foundation;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;
using SwiftDrop.Core.Storage;
using UIKit;

namespace SwiftDrop.ShareExtension;

[Register("ShareViewController")]
public sealed class ShareViewController : UIViewController
{
    private const int CopyBufferSize = 128 * 1024;
    private readonly CancellationTokenSource _lifetimeCts = new();
    private UILabel? _statusLabel;
    private int _started;

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        if (View is null) return;

        View.BackgroundColor = UIColor.SystemBackground;
        _statusLabel = new UILabel(View.Bounds)
        {
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
            Lines = 0,
            TextAlignment = UITextAlignment.Center,
            Text = "Adding to SwiftDrop…"
        };
        View.AddSubview(_statusLabel);
    }

    public override void ViewDidAppear(bool animated)
    {
        base.ViewDidAppear(animated);
        if (Interlocked.Exchange(ref _started, 1) != 0) return;
        _ = ProcessShareAsync(_lifetimeCts.Token);
    }

    public override void ViewDidDisappear(bool animated)
    {
        _lifetimeCts.Cancel();
        base.ViewDidDisappear(animated);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _lifetimeCts.Cancel();
            _lifetimeCts.Dispose();
        }
        base.Dispose(disposing);
    }

    private async Task ProcessShareAsync(CancellationToken ct)
    {
        var temporaryRoots = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            var context = ExtensionContext ?? throw new InvalidOperationException("Share extension context is unavailable.");
            var sources = new List<AppleSharedFileSource>();
            var texts = new List<string>();
            var providerCount = 0;

            foreach (var input in context.InputItems.OfType<NSExtensionItem>().Take(ExternalSharePackageConstants.MaximumItems))
            {
                ct.ThrowIfCancellationRequested();
                var attributedText = input.AttributedContentText?.Value;
                if (!string.IsNullOrWhiteSpace(attributedText)) texts.Add(attributedText);

                foreach (var provider in input.Attachments ?? Array.Empty<NSItemProvider>())
                {
                    ct.ThrowIfCancellationRequested();
                    if (++providerCount > ExternalSharePackageConstants.MaximumItems * 2) break;
                    if (sources.Count >= ExternalSharePackageConstants.MaximumItems) break;

                    var sharedFile = await TryLoadProviderFileAsync(provider, temporaryRoots);
                    ct.ThrowIfCancellationRequested();
                    if (sharedFile is not null)
                    {
                        sources.Add(sharedFile);
                        continue;
                    }

                    var text = await TryLoadProviderTextAsync(provider);
                    ct.ThrowIfCancellationRequested();
                    if (!string.IsNullOrWhiteSpace(text)) texts.Add(text);
                }
            }

            var combinedText = BuildBoundedText(texts, ProtocolConstants.MaxTextSnippetBytes);
            if (sources.Count == 0 && string.IsNullOrWhiteSpace(combinedText))
                throw new InvalidDataException("The selected share contains no supported file or text content.");

            await AppleSharePackageWriter.WriteAsync(sources, combinedText, ct);
            SetStatus("Added to SwiftDrop. Open SwiftDrop to review and send.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            SetStatus("SwiftDrop share cancelled.");
        }
        catch
        {
            SetStatus("SwiftDrop could not import this share safely.");
        }
        finally
        {
            foreach (var root in temporaryRoots) DeleteBestEffort(root);
            try { await Task.Delay(450, CancellationToken.None); } catch { }
            BeginInvokeOnMainThread(() =>
                ExtensionContext?.CompleteRequest(Array.Empty<NSExtensionItem>(), _ => { }));
        }
    }

    private static async Task<AppleSharedFileSource?> TryLoadProviderFileAsync(
        NSItemProvider provider,
        HashSet<string> temporaryRoots)
    {
        ArgumentNullException.ThrowIfNull(provider);

        if (provider.HasItemConformingTo("public.file-url"))
        {
            var fileUrl = await LoadFileUrlItemAsync(provider, "public.file-url", temporaryRoots);
            if (fileUrl is not null)
                return new AppleSharedFileSource(fileUrl, provider.SuggestedName);
        }

        var registered = provider.RegisteredTypeIdentifiers ?? Array.Empty<string>();
        var typeIdentifier = registered.FirstOrDefault(identifier => !IsTextOrUrlType(identifier));
        if (string.IsNullOrWhiteSpace(typeIdentifier)) return null;

        var staged = await LoadFileRepresentationAsync(provider, typeIdentifier, provider.SuggestedName, temporaryRoots);
        return staged is null ? null : new AppleSharedFileSource(staged, provider.SuggestedName);
    }

    private static Task<NSUrl?> LoadFileUrlItemAsync(
        NSItemProvider provider,
        string typeIdentifier,
        HashSet<string> temporaryRoots)
    {
        var tcs = new TaskCompletionSource<NSUrl?>(TaskCreationOptions.RunContinuationsAsynchronously);
        provider.LoadItem(typeIdentifier, null, (item, error) =>
        {
            if (error is not null || item is not NSUrl url || !url.IsFileUrl)
            {
                tcs.TrySetResult(null);
                return;
            }

            try
            {
                tcs.TrySetResult(CopyProviderFileToTemporaryStorage(url, provider.SuggestedName, temporaryRoots));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return tcs.Task;
    }

    private static Task<NSUrl?> LoadFileRepresentationAsync(
        NSItemProvider provider,
        string typeIdentifier,
        string? suggestedName,
        HashSet<string> temporaryRoots)
    {
        var tcs = new TaskCompletionSource<NSUrl?>(TaskCreationOptions.RunContinuationsAsynchronously);
        provider.LoadFileRepresentation(typeIdentifier, (url, error) =>
        {
            if (error is not null || url is null || !url.IsFileUrl)
            {
                tcs.TrySetResult(null);
                return;
            }

            try
            {
                tcs.TrySetResult(CopyProviderFileToTemporaryStorage(url, suggestedName, temporaryRoots));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return tcs.Task;
    }

    private static NSUrl CopyProviderFileToTemporaryStorage(
        NSUrl url,
        string? suggestedName,
        HashSet<string> temporaryRoots)
    {
        var granted = false;
        try
        {
            granted = url.StartAccessingSecurityScopedResource();
            var sourcePath = url.Path;
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new InvalidDataException("Shared provider file path is unavailable.");
            var source = new FileInfo(sourcePath);
            if (!source.Exists)
                throw new FileNotFoundException("Shared provider file is unavailable.", sourcePath);
            if (source.Length < 0 || source.Length > ProtocolConstants.MaxSingleFileBytes)
                throw new InvalidDataException("Shared provider file exceeds SwiftDrop limits.");

            var root = Path.Combine(Path.GetTempPath(), "SwiftDropShare", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            temporaryRoots.Add(root);
            StorageCapacityGuard.EnsureCapacity(root, source.Length);

            var requestedName = string.IsNullOrWhiteSpace(suggestedName) ? source.Name : suggestedName;
            var safeName = FileNameSanitizer.SanitizeSegment(requestedName);
            var destination = Path.Combine(root, safeName);

            using var input = new FileStream(source.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, CopyBufferSize, FileOptions.SequentialScan);
            if (input.Length != source.Length)
                throw new IOException("Shared provider file changed before staging.");
            using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, CopyBufferSize, FileOptions.SequentialScan);

            var remaining = source.Length;
            var buffer = new byte[CopyBufferSize];
            while (remaining > 0)
            {
                var requested = (int)Math.Min(buffer.Length, remaining);
                var read = input.Read(buffer, 0, requested);
                if (read == 0) throw new EndOfStreamException("Shared provider file ended unexpectedly.");
                output.Write(buffer, 0, read);
                remaining -= read;
            }
            output.Flush(flushToDisk: true);
            if (new FileInfo(source.FullName).Length != source.Length || new FileInfo(destination).Length != source.Length)
                throw new IOException("Shared provider file changed while staging.");

            return NSUrl.FromFilename(destination);
        }
        finally
        {
            if (granted) url.StopAccessingSecurityScopedResource();
        }
    }

    private static Task<string?> TryLoadProviderTextAsync(NSItemProvider provider)
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

            var value = item switch
            {
                NSString text => text.ToString(),
                NSUrl url when !url.IsFileUrl => url.AbsoluteString,
                _ => null
            };
            tcs.TrySetResult(value);
        });
        return tcs.Task;
    }

    private static string? BuildBoundedText(IEnumerable<string> values, int maximumUtf8Bytes)
    {
        var combined = string.Join(Environment.NewLine, values.Where(value => !string.IsNullOrWhiteSpace(value))).Trim();
        if (combined.Length == 0) return null;
        if (Encoding.UTF8.GetByteCount(combined) <= maximumUtf8Bytes) return combined;

        var low = 0;
        var high = combined.Length;
        while (low < high)
        {
            var mid = low + (high - low + 1) / 2;
            var end = mid;
            if (end > 0 && end < combined.Length && char.IsHighSurrogate(combined[end - 1]) && char.IsLowSurrogate(combined[end]))
                end--;
            if (Encoding.UTF8.GetByteCount(combined.AsSpan(0, end)) <= maximumUtf8Bytes) low = end;
            else high = mid - 1;
        }
        return combined[..low];
    }

    private static bool IsTextOrUrlType(string identifier)
        => identifier is "public.text" or "public.plain-text" or "public.utf8-plain-text" or "public.url" or "public.file-url";

    private void SetStatus(string text)
        => BeginInvokeOnMainThread(() =>
        {
            if (_statusLabel is not null) _statusLabel.Text = text;
        });

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
