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
                await DisplayAlert("Shared pairing link rejected", ex.Message, "OK");
            }
        }

        if (!string.IsNullOrWhiteSpace(input.SharedText))
        {
            TextSnippetEditor.Text = input.SharedText;
            TextTransferStatusLabel.Text = "Text was received from the platform share or drop surface. Review it before sending.";
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
            SelectedBatchLabel.Text = _selectedBatchFiles.Length switch
            {
                0 => "Shared files or folders were unavailable",
                1 when folderCount == 1 => $"Shared folder: {new DirectoryInfo(_selectedBatchFiles[0].FullPath).Name}",
                1 => $"Shared file: {_selectedBatchFiles[0].FileName}",
                _ => $"{fileCount:N0} shared file(s) and {folderCount:N0} folder(s) ready to send"
            };
            BatchTransferStatusLabel.Text = "Shared/dropped sources are selected locally. Review the selection and verify a receiving device before sending.";
        }
    }
}
