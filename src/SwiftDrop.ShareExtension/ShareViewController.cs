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
    private static readonly TimeSpan ProviderLoadTimeout = TimeSpan.FromSeconds(20);
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

                    var sharedFile = await TryLoadProviderFileAsync(provider, temporaryRoots, ct);
                    ct.ThrowIfCancellationRequested();
                    if (sharedFile is not null)
                    {
                        sources.Add(sharedFile);
                        continue;
                    }

                    var text = await TryLoadProviderTextAsync(provider, ct);
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
        HashSet<string> temporaryRoots,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ct.ThrowIfCancellationRequested();

        if (provider.HasItemConformingTo("public.file-url"))
        {
            var fileUrl = await LoadFileUrlItemAsync(provider, "public.file-url", temporaryRoots, ct);
            if (fileUrl is not null)
                return new AppleSharedFileSource(fileUrl, provider.SuggestedName);
        }

        var registered = provider.RegisteredTypeIdentifiers ?? Array.Empty<string>();
        var typeIdentifier = registered.FirstOrDefault(identifier => !IsTextOrUrlType(identifier));
        if (string.IsNullOrWhiteSpace(typeIdentifier)) return null;

        var staged = await LoadFileRepresentationAsync(provider, typeIdentifier, provider.SuggestedName, temporaryRoots, ct);
        return staged is null ? null : new AppleSharedFileSource(staged, provider.SuggestedName);
    }

    private static async Task<NSUrl?> LoadFileUrlItemAsync(
        NSItemProvider provider,
        string typeIdentifier,
        HashSet<string> temporaryRoots,
        CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(ProviderLoadTimeout);
        var providerToken = timeout.Token;
        var tcs = new TaskCompletionSource<NSUrl?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = providerToken.Register(() => tcs.TrySetCanceled(providerToken));

        provider.LoadItem(typeIdentifier, null, (item, error) =>
        {
            if (providerToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(providerToken);
                return;
            }
            if (error is not null || item is not NSUrl url || !url.IsFileUrl)
            {
                tcs.TrySetResult(null);
                return;
            }

            try
            {
                tcs.TrySetResult(CopyProviderFileToTemporaryStorage(
                    url,
                    provider.SuggestedName,
                    temporaryRoots,
                    providerToken));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        try
        {
            return await tcs.Task;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("Shared provider did not return the file URL before the safety timeout.");
        }
    }

    private static async Task<NSUrl?> LoadFileRepresentationAsync(
        NSItemProvider provider,
        string typeIdentifier,
        string? suggestedName,
        HashSet<string> temporaryRoots,
        CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(ProviderLoadTimeout);
        var providerToken = timeout.Token;
        var tcs = new TaskCompletionSource<NSUrl?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = providerToken.Register(() => tcs.TrySetCanceled(providerToken));

        provider.LoadFileRepresentation(typeIdentifier, (url, error) =>
        {
            if (providerToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(providerToken);
                return;
            }
            if (error is not null || url is null || !url.IsFileUrl)
            {
                tcs.TrySetResult(null);
                return;
            }

            try
            {
                tcs.TrySetResult(CopyProviderFileToTemporaryStorage(
                    url,
                    suggestedName,
                    temporaryRoots,
                    providerToken));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        try
        {
            return await tcs.Task;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("Shared provider did not return a file representation before the safety timeout.");
        }
    }

    private static NSUrl CopyProviderFileToTemporaryStorage(
        NSUrl url,
        string? suggestedName,
        HashSet<string> temporaryRoots,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var granted = false;
        try
        {
            granted = url.StartAccessingSecurityScopedResource();
            ct.ThrowIfCancellationRequested();
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
                ct.ThrowIfCancellationRequested();
                var requested = (int)Math.Min(buffer.Length, remaining);
                var read = input.Read(buffer, 0, requested);
                if (read == 0) throw new EndOfStreamException("Shared provider file ended unexpectedly.");
                output.Write(buffer, 0, read);
                remaining -= read;
            }
            output.Flush(flushToDisk: true);
            ct.ThrowIfCancellationRequested();
            if (new FileInfo(source.FullName).Length != source.Length || new FileInfo(destination).Length != source.Length)
                throw new IOException("Shared provider file changed while staging.");

            return NSUrl.FromFilename(destination);
        }
        finally
        {
            if (granted) url.StopAccessingSecurityScopedResource();
        }
    }

    private static async Task<string?> TryLoadProviderTextAsync(NSItemProvider provider, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var typeIdentifier = provider.HasItemConformingTo("public.plain-text")
            ? "public.plain-text"
            : provider.HasItemConformingTo("public.text")
                ? "public.text"
                : provider.HasItemConformingTo("public.url")
                    ? "public.url"
                    : null;
        if (typeIdentifier is null) return null;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(ProviderLoadTimeout);
        var providerToken = timeout.Token;
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = providerToken.Register(() => tcs.TrySetCanceled(providerToken));

        provider.LoadItem(typeIdentifier, null, (item, error) =>
        {
            if (providerToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(providerToken);
                return;
            }
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

        try
        {
            return await tcs.Task;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("Shared provider did not return text before the safety timeout.");
        }
    }

    private static string? BuildBoundedText(IEnumerable<string> values, int maximumUtf8Bytes)
    {
        var combined = string.Join(Environment.NewLine, values.Where(value => !string.IsNullOrWhiteSpace(value))).Trim();
        return combined.Length == 0 ? null : Utf8TextLimiter.Truncate(combined, maximumUtf8Bytes);
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
