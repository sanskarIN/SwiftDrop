using SwiftDrop.App.Services;
using SwiftDrop.Core.Security;

namespace SwiftDrop.App;

public partial class MainPage
{
    public async Task ApplyExternalInputAsync()
    {
        var input = ExternalInputInbox.Drain();
        if (!input.HasAny) return;

        if (!string.IsNullOrWhiteSpace(input.PairingLink))
        {
            try
            {
                RemoteLinkEntry.Text = input.PairingLink;
                await ConfirmRemotePairingAsync(PairingCodec.Decode(input.PairingLink));
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    AppText.Get("SharedPairingLinkRejected"),
                    ex.Message,
                    AppText.Get("Ok"));
            }
        }

        if (!string.IsNullOrWhiteSpace(input.SharedText))
        {
            TextSnippetEditor.Text = input.SharedText;
            _viewModel.TextTransferStatus = AppText.Get("ExternalTextReviewStatus");
        }

        if (input.SharedFiles.Count > 0)
        {
            _selectedBatchFiles = input.SharedFiles
                .Where(path => File.Exists(path) || Directory.Exists(path))
                .Take(2048)
                .Select(path => new FileResult(path))
                .ToArray();

            var folderCount = _selectedBatchFiles.Count(x => Directory.Exists(x.FullPath));
            var fileCount = _selectedBatchFiles.Length - folderCount;
            _viewModel.SelectedBatch = _selectedBatchFiles.Length switch
            {
                0 => AppText.Get("SharedSourcesUnavailable"),
                1 when folderCount == 1 => AppText.Format(
                    "SharedFolderFormat",
                    new DirectoryInfo(_selectedBatchFiles[0].FullPath).Name),
                1 => AppText.Format("SharedFileFormat", _selectedBatchFiles[0].FileName),
                _ => AppText.Format("SharedSourcesReadyFormat", fileCount, folderCount)
            };
            _viewModel.BatchTransferStatus = AppText.Get("ExternalSourcesReviewStatus");
        }
    }
}
