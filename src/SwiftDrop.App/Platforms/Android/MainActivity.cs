using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using SwiftDrop.App.Services;

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
            if (!string.IsNullOrWhiteSpace(text)) ExternalInputInbox.SetSharedText(text);

            var uris = new List<Android.Net.Uri>();
            var clip = intent.ClipData;
            if (clip is not null)
            {
                for (var i = 0; i < clip.ItemCount; i++)
                {
                    var uri = clip.GetItemAt(i)?.Uri;
                    if (uri is not null) uris.Add(uri);
                }
            }

#pragma warning disable CS0618
            var single = intent.GetParcelableExtra(Intent.ExtraStream) as Android.Net.Uri;
            if (single is not null && uris.All(x => x != single)) uris.Add(single);
            var multiple = intent.GetParcelableArrayListExtra(Intent.ExtraStream);
#pragma warning restore CS0618
            if (multiple is not null)
            {
                foreach (var item in multiple)
                {
                    if (item is Android.Net.Uri uri && uris.All(x => x != uri)) uris.Add(uri);
                }
            }

            foreach (var uri in uris.Take(2048))
            {
                var staged = await StageSharedUriAsync(uri);
                if (staged is not null) ExternalInputInbox.AddSharedFile(staged);
            }
        }
        catch
        {
            // External shares are optional input. Failure must not crash app startup.
        }
    }

    private async Task<string?> StageSharedUriAsync(Android.Net.Uri uri)
    {
        using var input = ContentResolver?.OpenInputStream(uri);
        if (input is null) return null;

        var displayName = GetDisplayName(uri);
        var safeName = SanitizeFileName(displayName);
        var directory = Path.Combine(FileSystem.CacheDirectory, "shared-input");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{Guid.NewGuid():N}-{safeName}");
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await input.CopyToAsync(output);
        await output.FlushAsync();
        return path;
    }

    private string GetDisplayName(Android.Net.Uri uri)
    {
        try
        {
            using var cursor = ContentResolver?.Query(uri, new[] { OpenableColumns.DisplayName }, null, null, null);
            if (cursor is not null && cursor.MoveToFirst())
            {
                var index = cursor.GetColumnIndex(OpenableColumns.DisplayName);
                if (index >= 0)
                {
                    var name = cursor.GetString(index);
                    if (!string.IsNullOrWhiteSpace(name)) return name;
                }
            }
        }
        catch
        {
        }
        return Path.GetFileName(uri.Path) is { Length: > 0 } fallback ? fallback : "shared-file";
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var filtered = new string(value.Where(c => !invalid.Contains(c) && !char.IsControl(c)).Take(180).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(filtered) ? "shared-file" : filtered;
    }
}
