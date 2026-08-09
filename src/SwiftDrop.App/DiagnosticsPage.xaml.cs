using SwiftDrop.App.Services;
using SwiftDrop.App.ViewModels;

namespace SwiftDrop.App;

public partial class DiagnosticsPage : ContentPage
{
    private readonly DiagnosticsViewModel _viewModel;

    public DiagnosticsPage(DiagnosticsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        Loaded += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            await _viewModel.RefreshAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(AppText.Get("Diagnostics"), $"Diagnostics refresh failed with {ex.GetType().Name}.", "OK");
        }
    }

    private async void RefreshClicked(object? sender, EventArgs e) => await RefreshAsync();

    private async void ExportClicked(object? sender, EventArgs e)
    {
        try
        {
            var path = await _viewModel.ExportAsync();
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "SwiftDrop safe diagnostics",
                File = new ShareFile(path)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Export failed", $"Diagnostics export failed with {ex.GetType().Name}.", "OK");
        }
    }

    private async void ClearLogClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            "Clear diagnostic log?",
            "This removes locally stored diagnostic events. Transfer history and received files are not changed.",
            "Clear",
            AppText.Get("Cancel"));
        if (!confirmed) return;
        await _viewModel.ClearEventsAsync();
    }

    private async void RunRoundTripTestClicked(object? sender, EventArgs e)
        => await RunSelfTestAsync(_viewModel.RunRoundTripAsync);

    private async void RunChecksumTestClicked(object? sender, EventArgs e)
        => await RunSelfTestAsync(_viewModel.RunChecksumMismatchAsync);

    private async void RunInterruptionTestClicked(object? sender, EventArgs e)
        => await RunSelfTestAsync(_viewModel.RunInterruptedReceiveAsync);

    private async Task RunSelfTestAsync(Func<CancellationToken, Task> action)
    {
        try
        {
            await action(CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            await DisplayAlertAsync("Developer options disabled", "Enable safe developer diagnostics in Settings first.", "OK");
        }
        catch (OperationCanceledException)
        {
        }
    }
}
