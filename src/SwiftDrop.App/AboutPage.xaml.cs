namespace SwiftDrop.App;

public partial class AboutPage : ContentPage
{
    private static readonly Uri RepositoryUri = new("https://github.com/sanskarIN/SwiftDrop");
    private static readonly Uri ProfileUri = new("https://www.github.com/sanskarIN");

    public AboutPage()
    {
        InitializeComponent();
        var version = AppInfo.Current.VersionString;
        var build = AppInfo.Current.BuildString;
        VersionLabel.Text = $"Version {version} • Build {build}";
    }

    private static Task OpenRepositoryClicked(object? sender, EventArgs e)
        => Launcher.Default.OpenAsync(RepositoryUri);

    private static Task OpenProfileClicked(object? sender, EventArgs e)
        => Launcher.Default.OpenAsync(ProfileUri);
}
