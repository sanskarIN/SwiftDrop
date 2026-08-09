using SwiftDrop.App.Services;

namespace SwiftDrop.App;

public partial class HistoryPage : ContentPage
{
    private readonly TransferHistoryService _history;

    public HistoryPage(TransferHistoryService history)
    {
        InitializeComponent();
        _history = history;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            await _history.InitializeAsync();
            HistoryList.ItemsSource = await _history.GetRecentAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("History error", ex.Message, "OK");
        }
    }

    private async void RefreshClicked(object? sender, EventArgs e) => await LoadAsync();

    private async void ClearClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlert("Clear history", "Delete all local transfer history? This does not delete received files.", "Clear", "Cancel");
        if (!confirm) return;
        await _history.ClearAsync();
        await LoadAsync();
    }
}
