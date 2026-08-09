using QRCoder;
using SwiftDrop.App.Services;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;

namespace SwiftDrop.App;

public partial class MainPage : ContentPage
{
    private readonly DeviceIdentityService _identity;
    private readonly TransferCoordinator _transfers;
    private PairingPayload? _remote;
    private FileResult? _selectedFile;
    private ReceiveServerService? _receiveServer;

    public MainPage(DeviceIdentityService identity, TransferCoordinator transfers)
    {
        InitializeComponent();
        _identity = identity;
        _transfers = transfers;
        Loaded += async (_, _) => await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await _identity.InitializeAsync();
            DeviceNameLabel.Text = _identity.DeviceName;
            DeviceIdLabel.Text = _identity.DeviceId;
            if (_receiveServer is null)
            {
                var receiveRoot = Path.Combine(FileSystem.AppDataDirectory, "Received");
                _receiveServer = new ReceiveServerService(_identity.Certificate, receiveRoot, _identity.TryConsumePairingNonce);
                _receiveServer.Start();
                TransferStatusLabel.Text = $"Ready to receive into {receiveRoot}";
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Startup error", ex.Message, "OK");
        }
    }

    private void CreatePairingClicked(object? sender, EventArgs e)
    {
        var link = _identity.CreatePairingLink();
        PairingLinkEntry.Text = link;
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(link, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(8);
        QrImage.Source = ImageSource.FromStream(() => new MemoryStream(png));
        QrImage.IsVisible = true;
    }

    private async void CopyLinkClicked(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(PairingLinkEntry.Text)) await Clipboard.Default.SetTextAsync(PairingLinkEntry.Text);
    }

    private async void ShareLinkClicked(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(PairingLinkEntry.Text))
            await Share.Default.RequestAsync(new ShareTextRequest { Text = PairingLinkEntry.Text, Title = "SwiftDrop pairing link" });
    }

    private async void ValidatePairingClicked(object? sender, EventArgs e)
    {
        try
        {
            _remote = PairingCodec.Decode(RemoteLinkEntry.Text ?? string.Empty);
            RemotePeerLabel.Text = $"{_remote.DeviceName} • {_remote.Host}:{_remote.Port}\nFingerprint: {Fingerprint.Pretty(_remote.CertificateFingerprint)}";
        }
        catch (Exception ex)
        {
            _remote = null;
            await DisplayAlert("Pairing failed", ex.Message, "OK");
        }
    }

    private async void ChooseFileClicked(object? sender, EventArgs e)
    {
        _selectedFile = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Choose a file to send" });
        SelectedFileLabel.Text = _selectedFile?.FullPath ?? "No file selected";
    }

    private async void SendFileClicked(object? sender, EventArgs e)
    {
        if (_remote is null) { await DisplayAlert("Device required", "Validate a pairing link first.", "OK"); return; }
        if (_selectedFile is null) { await DisplayAlert("File required", "Choose a file first.", "OK"); return; }
        try
        {
            TransferStatusLabel.Text = "Sending…";
            var progress = new Progress<double>(value => TransferProgress.Progress = value);
            await _transfers.SendAsync(_remote, _selectedFile.FullPath, progress, CancellationToken.None);
            TransferStatusLabel.Text = "Completed";
        }
        catch (Exception ex)
        {
            TransferStatusLabel.Text = "Failed";
            await DisplayAlert("Transfer failed", ex.Message, "OK");
        }
    }
}
