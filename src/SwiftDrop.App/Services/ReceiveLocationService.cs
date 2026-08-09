namespace SwiftDrop.App.Services;

public sealed class ReceiveLocationService
{
    private readonly AppSettingsService _settings;

    public ReceiveLocationService(AppSettingsService settings)
    {
        _settings = settings;
    }

    public string ResolveReceiveRoot()
    {
        var configured = _settings.Load().DefaultReceiveFolder;
        var path = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(FileSystem.AppDataDirectory, "Received")
            : configured;
        var full = Path.GetFullPath(path);
        Directory.CreateDirectory(full);
        return full;
    }

    public string GetDefaultAppReceiveRoot()
        => Path.Combine(FileSystem.AppDataDirectory, "Received");

    public async Task<string?> PickFolderAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
#if WINDOWS
        var window = Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (window is null) throw new InvalidOperationException("SwiftDrop could not access the active Windows window.");

        var picker = new Windows.Storage.Pickers.FolderPicker();
        picker.FileTypeFilter.Add("*");
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        var folder = await picker.PickSingleFolderAsync();
        ct.ThrowIfCancellationRequested();
        return folder?.Path;
#else
        await Task.CompletedTask;
        return null;
#endif
    }
}
