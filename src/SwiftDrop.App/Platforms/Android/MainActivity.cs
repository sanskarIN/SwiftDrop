using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using SwiftDrop.App.Services;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;
using SwiftDrop.Core.Storage;

namespace SwiftDrop.App;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTask,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "swiftdrop",
    DataHost = "pair")]
[IntentFilter(
    new[] { Intent.ActionSend },
    Categories = new[] { Intent.CategoryDefault },
    DataMimeType = "*/*")]
[IntentFilter(
    new[] { Intent.ActionSendMultiple },
    Categories = new[] { Intent.CategoryDefault },
    DataMimeType = "*/*")]
public class MainActivity : MauiAppCompatActivity
{
    private const int BufferSize = 128 * 1024;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _ = ProcessExternalIntentAsync(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        if (intent is null) return;
        SetIntent(intent);
        _ = ProcessExternalIntentAsync(intent);
    }

    private async Task ProcessExternalIntentAsync(Intent? intent)
    {
        if (intent is null) return;
        try
        {
            if (string.Equals(intent.Action, Intent.ActionView, StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(intent.DataString))
            {
                ExternalInputInbox.SetPairingLink(intent.DataString);
                return;
            }

            if (intent.Action is not (Intent.ActionSend or Intent.ActionSendMultiple)) return;

            var text = intent.GetStringExtra(Intent.ExtraText);
            var uris = CollectSharedUris(intent);
            var stagedPaths = new List<string>(Math.Min(uris.Count, ProtocolConstants.MaxBatchFiles));
            foreach (var uri in uris.Take(ProtocolConstants.MaxBatchFiles))
            {
                var staged = await StageSharedUriAsync(uri);
                if (staged is not null) stagedPaths.Add(staged);
            }

            if (!string.IsNullOrWhiteSpace(text) || stagedPaths.Count > 0)
                ExternalInputInbox.AddSharedBatch(text, stagedPaths);
        }
        catch
        {
            // External shares are optional input. Failure must not crash app startup.
        }
    }

    private static List<Android.Net.Uri> CollectSharedUris(Intent intent)
    {
        var uris = new List<Android.Net.Uri>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Add(Android.Net.Uri? uri)
        {
            if (uri is null || uris.Count >= ProtocolConstants.MaxBatchFiles) return;
            var key = uri.ToString();
            if (!string.IsNullOrWhiteSpace(key) && seen.Add(key)) uris.Add(uri);
        }

        var clip = intent.ClipData;
        if (clip is not null)
        {
            for (var i = 0; i < clip.ItemCount && uris.Count < ProtocolConstants.MaxBatchFiles; i++)
                Add(clip.GetItemAt(i)?.Uri);
        }

#pragma warning disable CS0618
        Add(intent.GetParcelableExtra(Intent.ExtraStream) as Android.Net.Uri);
        var multiple = intent.GetParcelableArrayListExtra(Intent.ExtraStream);
#pragma warning restore CS0618
        if (multiple is not null)
        {
            foreach (var item in multiple)
            {
                if (item is Android.Net.Uri uri) Add(uri);
                if (uris.Count >= ProtocolConstants.MaxBatchFiles) break;
            }
        }

        return uris;
    }

    private async Task<string?> StageSharedUriAsync(Android.Net.Uri uri)
    {
        string? path = null;
        try
        {
            var metadata = GetMetadata(uri);
            if (metadata.DeclaredLength is < 0 or > ProtocolConstants.MaxSingleFileBytes)
                return null;

            using var input = ContentResolver?.OpenInputStream(uri);
            if (input is null) return null;

            var safeName = FileNameSanitizer.SanitizeSegment(metadata.DisplayName);
            var directory = Path.Combine(FileSystem.CacheDirectory, "shared-input");
            Directory.CreateDirectory(directory);
            path = Path.Combine(directory, $"{Guid.NewGuid():N}-{safeName}");
            StorageCapacityGuard.EnsureCapacity(path, metadata.DeclaredLength ?? 0);

            await using var output = new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            var buffer = new byte[BufferSize];
            long total = 0;
            while (true)
            {
                var read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length));
                if (read == 0) break;
                total = checked(total + read);
                if (total > ProtocolConstants.MaxSingleFileBytes)
                    throw new InvalidDataException("Shared Android item exceeds SwiftDrop file limits.");
                await output.WriteAsync(buffer.AsMemory(0, read));
            }
            await output.FlushAsync();

            if (metadata.DeclaredLength is long expected && total != expected)
                throw new IOException("Shared Android item length changed while staging.");
            if (new FileInfo(path).Length != total)
                throw new IOException("Shared Android staging length mismatch.");

            var completed = path;
            path = null;
            return completed;
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(path)) DeleteBestEffort(path);
            return null;
        }
    }

    private SharedUriMetadata GetMetadata(Android.Net.Uri uri)
    {
        var fallback = Path.GetFileName(uri.Path) is { Length: > 0 } name ? name : "shared-file";
        try
        {
            using var cursor = ContentResolver?.Query(
                uri,
                new[] { OpenableColumns.DisplayName, OpenableColumns.Size },
                null,
                null,
                null);
            if (cursor is not null && cursor.MoveToFirst())
            {
                var displayIndex = cursor.GetColumnIndex(OpenableColumns.DisplayName);
                var sizeIndex = cursor.GetColumnIndex(OpenableColumns.Size);
                var displayName = displayIndex >= 0 && !cursor.IsNull(displayIndex)
                    ? cursor.GetString(displayIndex)
                    : fallback;
                long? length = sizeIndex >= 0 && !cursor.IsNull(sizeIndex)
                    ? cursor.GetLong(sizeIndex)
                    : null;
                return new SharedUriMetadata(
                    string.IsNullOrWhiteSpace(displayName) ? fallback : displayName,
                    length);
            }
        }
        catch
        {
        }
        return new SharedUriMetadata(fallback, null);
    }

    private static void DeleteBestEffort(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
        }
    }

    private sealed record SharedUriMetadata(string DisplayName, long? DeclaredLength);
}
