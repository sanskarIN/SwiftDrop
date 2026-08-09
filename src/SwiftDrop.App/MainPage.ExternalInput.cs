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
            TextTransferStatusLabel.Text = "Text was received from the platform share sheet. Review it before sending.";
        }

        if (input.SharedFiles.Count > 0)
        {
            _selectedBatchFiles = input.SharedFiles
                .Where(File.Exists)
                .Take(2048)
                .Select(path => new FileResult(path))
                .ToArray();
            SelectedBatchLabel.Text = _selectedBatchFiles.Length switch
            {
                0 => "Shared files were unavailable",
                1 => $"Shared file: {_selectedBatchFiles[0].FileName}",
                _ => $"{_selectedBatchFiles.Length:N0} shared files ready to send"
            };
            BatchTransferStatusLabel.Text = "Shared files were staged in SwiftDrop cache. Choose/verify a receiving device before sending.";
        }
    }
}
