using SwiftDrop.App.Services;

namespace SwiftDrop.App;

public partial class MainPage
{
    private async void ChooseFolderClicked(object? sender, EventArgs e)
    {
        try
        {
            var selected = await _receiveLocation.PickFolderAsync();
            if (string.IsNullOrWhiteSpace(selected))
            {
                await DisplayAlert(
                    AppText.Get("FolderPickerUnavailable"),
                    AppText.Get("FolderPickerUnavailableDetailed"),
                    AppText.Get("Ok"));
                return;
            }

            _selectedBatchFiles = new[] { new FileResult(selected) };
            _viewModel.SelectedBatch = AppText.Format(
                "FolderSelectedFormat",
                new DirectoryInfo(selected).Name);
            _viewModel.BatchTransferStatus = AppText.Get("FolderRecursiveStatus");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                AppText.Get("FolderSelectionFailed"),
                ex.Message,
                AppText.Get("Ok"));
        }
    }
}
