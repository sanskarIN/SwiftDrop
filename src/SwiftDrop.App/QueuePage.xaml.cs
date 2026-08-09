using SwiftDrop.App.ViewModels;

namespace SwiftDrop.App;

public partial class QueuePage : ContentPage
{
    private readonly QueueViewModel _viewModel;

    public QueuePage(QueueViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
        => await _viewModel.InitializeAsync();

    private void OnUnloaded(object? sender, EventArgs e)
        => _viewModel.Dispose();

    private void RefreshClicked(object? sender, EventArgs e)
        => _viewModel.Refresh();

    private async void ClearFinishedClicked(object? sender, EventArgs e)
        => await _viewModel.ClearFinishedAsync();
}
