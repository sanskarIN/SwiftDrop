using SwiftDrop.App.ViewModels;

namespace SwiftDrop.App;

public partial class HistoryPage : ContentPage
{
    private readonly HistoryViewModel _viewModel;

    public HistoryPage(HistoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try { await _viewModel.RefreshAsync(); }
        catch (Exception ex) { await DisplayAlert("History error", ex.Message, "OK"); }
    }

    private async void RefreshClicked(object? sender, EventArgs e) => await LoadAsync();

    private async void DeleteClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not string id) return;
        var confirm = await DisplayAlert(
            "Delete history item",
            "Delete this local history record? This does not delete transferred or received content.",
            "Delete",
            "Cancel");
        if (!confirm) return;
        await _viewModel.DeleteAsync(id);
    }

    private async void ClearClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlert(
            "Clear history",
            "Delete all local transfer history? This does not delete received files.",
            "Clear",
            "Cancel");
        if (!confirm) return;
        await _viewModel.ClearAsync();
    }
}
