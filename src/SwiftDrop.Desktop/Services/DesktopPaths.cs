namespace SwiftDrop.Desktop.Services;

public static class DesktopPaths
{
    public static string ConfigRoot { get; } = ResolveRoot("XDG_CONFIG_HOME", ".config");
    public static string DataRoot { get; } = ResolveRoot("XDG_DATA_HOME", Path.Combine(".local", "share"));
    public static string CacheRoot { get; } = ResolveRoot("XDG_CACHE_HOME", ".cache");

    public static string DefaultReceiveRoot
    {
        get
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var downloads = string.IsNullOrWhiteSpace(home) ? string.Empty : Path.Combine(home, "Downloads");
            return Directory.Exists(downloads)
                ? Path.Combine(downloads, "SwiftDrop")
                : Path.Combine(DataRoot, "received");
        }
    }

    public static void EnsurePrivateDirectory(string path)
    {
        Directory.CreateDirectory(path);
        if (OperatingSystem.IsWindows()) return;

        try
        {
            File.SetUnixFileMode(
                path,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (PlatformNotSupportedException)
        {
        }
    }

    public static void RestrictPrivateFile(string path)
    {
        if (OperatingSystem.IsWindows()) return;

        try
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (PlatformNotSupportedException)
        {
        }
    }

    private static string ResolveRoot(string variable, string unixFallback)
    {
        var explicitRoot = Environment.GetEnvironmentVariable(variable);
        if (!string.IsNullOrWhiteSpace(explicitRoot))
            return Path.GetFullPath(Path.Combine(explicitRoot, "swiftdrop"));

        if (OperatingSystem.IsWindows())
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(local))
                return Path.Combine(local, "SwiftDrop");
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(home))
            home = AppContext.BaseDirectory;
        return Path.GetFullPath(Path.Combine(home, unixFallback, "swiftdrop"));
    }
}
