using SwiftDrop.App.Services;
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
        try
        {
            await _viewModel.RefreshAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(AppText.Get("HistoryError"), ex.Message, AppText.Get("Ok"));
        }
    }

    private async void RefreshClicked(object? sender, EventArgs e) => await LoadAsync();

    private async void DeleteClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not string id) return;
        var confirm = await DisplayAlertAsync(
            AppText.Get("DeleteHistoryItem"),
            AppText.Get("DeleteHistoryItemMessage"),
            AppText.Get("Delete"),
            AppText.Get("Cancel"));
        if (!confirm) return;
        await _viewModel.DeleteAsync(id);
    }

    private async void ClearClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync(
            AppText.Get("ClearHistoryQuestion"),
            AppText.Get("ClearHistoryMessage"),
            AppText.Get("Clear"),
            AppText.Get("Cancel"));
        if (!confirm) return;
        await _viewModel.ClearAsync();
    }
}
