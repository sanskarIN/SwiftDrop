using SwiftDrop.App.Services;
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
        => await OpenExternalAsync(RepositoryUri, AppText.Get("RepositoryLinkLabel"));

    private async void OpenProfileClicked(object? sender, EventArgs e)
        => await OpenExternalAsync(ProfileUri, AppText.Get("CreatorProfileLinkLabel"));

    private async void OpenBuyMeACoffeeClicked(object? sender, EventArgs e)
        => await OpenExternalAsync(BuyMeACoffeeUri, AppText.Get("BuyMeACoffeeLinkLabel"));

    private async Task OpenExternalAsync(Uri uri, string label)
    {
        try
        {
            if (!await Launcher.Default.TryOpenAsync(uri))
            {
                await DisplayAlertAsync(
                    AppText.Get("UnableToOpenLink"),
                    AppText.Format("UnableToOpenLinkFormat", label),
                    AppText.Get("Ok"));
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
        {
            await DisplayAlertAsync(
                AppText.Get("UnableToOpenLink"),
                AppText.Format("OpeningLinkUnsupportedFormat", label),
                AppText.Get("Ok"));
        }
    }
}
