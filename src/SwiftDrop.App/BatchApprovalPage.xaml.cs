using System.ComponentModel;
using System.Runtime.CompilerServices;
using SwiftDrop.App.Services;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;

namespace SwiftDrop.App;

public partial class BatchApprovalPage : ContentPage
{
    private readonly IncomingBatchPreview _preview;
    private readonly BatchApprovalRow[] _rows;
    private readonly TaskCompletionSource<IncomingBatchDecision> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public BatchApprovalPage(IncomingBatchPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        InitializeComponent();
        _preview = preview;
        _rows = preview.Files.Select(BatchApprovalRow.FromEntry).ToArray();
        FilesList.ItemsSource = _rows;
        SenderLabel.Text = $"From {preview.SenderDeviceName}\nCertificate: {Fingerprint.Pretty(preview.SenderCertificateFingerprint)}";
        SummaryLabel.Text = $"{preview.FileCount:N0} files • {FormatBytes(preview.TotalBytes)} total";
        RiskLabel.Text = preview.HighestRisk switch
        {
            FileRiskLevel.High => "Warning: at least one selected item can execute code or install software. Review the list carefully.",
            FileRiskLevel.Caution => "Caution: at least one selected item is an archive, disk image, or active-content file.",
            _ => "No high-risk extension was identified. Extension checks are warnings only, not malware scanning."
        };
    }

    public Task<IncomingBatchDecision> DecisionTask => _completion.Task;

    private async void RejectClicked(object? sender, EventArgs e)
        => await CompleteAndCloseAsync(IncomingBatchDecision.Reject);

    private async void AcceptSelectedClicked(object? sender, EventArgs e)
    {
        var selected = _rows.Where(x => x.IsSelected).Select(x => x.RelativePath).ToHashSet(StringComparer.Ordinal);
        if (selected.Count == 0)
        {
            await DisplayAlert("Nothing selected", "Select at least one file or reject the transfer.", "OK");
            return;
        }

        await CompleteAndCloseAsync(new IncomingBatchDecision(true, selected));
    }

    private async void AcceptAllClicked(object? sender, EventArgs e)
        => await CompleteAndCloseAsync(IncomingBatchDecision.AcceptAll(_preview.Files));

    private async Task CompleteAndCloseAsync(IncomingBatchDecision decision)
    {
        if (!_completion.TrySetResult(decision)) return;
        await Navigation.PopModalAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        _completion.TrySetResult(IncomingBatchDecision.Reject);
        return base.OnBackButtonPressed();
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return $"{value:0.##} {units[unit]}";
    }

    public sealed class BatchApprovalRow : INotifyPropertyChanged
    {
        private bool _isSelected = true;

        private BatchApprovalRow(string relativePath, long sizeBytes, FileRiskLevel risk)
        {
            RelativePath = relativePath;
            SizeBytes = sizeBytes;
            Risk = risk;
        }

        public string RelativePath { get; }
        public long SizeBytes { get; }
        public FileRiskLevel Risk { get; }
        public string SizeText => FormatBytes(SizeBytes);
        public string RiskText => Risk switch
        {
            FileRiskLevel.High => "High-risk extension",
            FileRiskLevel.Caution => "Caution extension",
            _ => "Normal extension"
        };

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public static BatchApprovalRow FromEntry(FileManifestEntry entry)
            => new(entry.RelativePath, entry.Length, FileRiskClassifier.Classify(entry.RelativePath));
    }
}
