using Microsoft.Extensions.DependencyInjection;

namespace SwiftDrop.App;

public partial class MainPage
{
    private async void OpenAboutClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(_services.GetRequiredService<AboutPage>());
}
