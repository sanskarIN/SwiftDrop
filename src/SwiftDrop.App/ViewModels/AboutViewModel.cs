namespace SwiftDrop.App.ViewModels;

public sealed class AboutViewModel
{
    public string VersionText { get; } =
        $"Version {AppInfo.Current.VersionString} • Build {AppInfo.Current.BuildString}";
}
