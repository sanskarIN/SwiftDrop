using SwiftDrop.App.ViewModels;

namespace SwiftDrop.App;

public partial class AboutPage : ContentPage
{
    private static readonly Uri RepositoryUri = new("https://github.com/sanskarIN/SwiftDrop");
    private static readonly Uri ProfileUri = new("https://www.github.com/sanskarIN");
    private static readonly Uri BuyMeACoffeeUri = new("https://buymeacoffee.com/sanskarIN");

    public AboutPage(AboutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OpenRepositoryClicked(object? sender, EventArgs e)
        => await OpenExternalAsync(RepositoryUri, "repository");

    private async void OpenProfileClicked(object? sender, EventArgs e)
        => await OpenExternalAsync(ProfileUri, "creator profile");

    private async void OpenBuyMeACoffeeClicked(object? sender, EventArgs e)
        => await OpenExternalAsync(BuyMeACoffeeUri, "Buy Me a Coffee page");

    private async Task OpenExternalAsync(Uri uri, string label)
    {
        try
        {
            if (!await Launcher.Default.TryOpenAsync(uri))
                await DisplayAlertAsync("Unable to open link", $"SwiftDrop could not open the {label} on this device.", "OK");
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
        {
            await DisplayAlertAsync("Unable to open link", $"Opening the {label} is not supported on this device.", "OK");
        }
    }
}
