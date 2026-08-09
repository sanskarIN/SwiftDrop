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
                    "Folder picker unavailable",
                    "This platform does not currently expose a SwiftDrop folder picker without broader storage integration. You can still choose multiple files, use a share sheet where supported, or send a folder from Windows.",
                    "OK");
                return;
            }

            _selectedBatchFiles = new[] { new FileResult(selected) };
            SelectedBatchLabel.Text = $"Folder: {new DirectoryInfo(selected).Name}";
            BatchTransferStatusLabel.Text = "The folder will be enumerated recursively when sending. Empty directories are not transferred because SwiftDrop transfers file content and relative paths.";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Folder selection failed", ex.Message, "OK");
        }
    }
}
