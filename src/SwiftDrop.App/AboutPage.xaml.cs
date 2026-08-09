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

    private async void OpenRepositoryClicked(object? sender, EventArgs e)
        => await Launcher.Default.OpenAsync(RepositoryUri);

    private async void OpenProfileClicked(object? sender, EventArgs e)
        => await Launcher.Default.OpenAsync(ProfileUri);
}
