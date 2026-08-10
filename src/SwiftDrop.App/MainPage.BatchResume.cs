using System.Diagnostics;
using SwiftDrop.App.Services;
using SwiftDrop.Core.Models;

namespace SwiftDrop.App;

public partial class MainPage
{
    private string? _pausedBatchTransferId;

    private async void SendBatchStableClicked(object? sender, EventArgs e)
    {
        if (_selectedBatchFiles.Length == 0)
        {
            await DisplayAlert(AppText.Get("FilesRequired"), AppText.Get("ChooseFilesFirst"), AppText.Get("Ok"));
            return;
        }
        if (!TryTakeRemote(out var remote))
        {
            await DisplayAlert(
                AppText.Get("DeviceRequired"),
                AppText.Get("FreshPairingInvitationFirst"),
                AppText.Get("Ok"));
            return;
        }

        _pausedBatchPaths = Array.Empty<string>();
        _pausedBatchTransferId = null;
        var transferId = Guid.NewGuid().ToString("N");
        await RunStableBatchSendAsync(
            remote,
            _selectedBatchFiles.Select(x => x.FullPath).ToArray(),
            transferId,
            isResume: false);
    }

    private async void ResumeBatchStableClicked(object? sender, EventArgs e)
    {
        var transferId = _pausedBatchTransferId;
        var paths = _pausedBatchPaths.Where(SourceExists).ToArray();
        if (paths.Length == 0 || string.IsNullOrWhiteSpace(transferId))
        {
            ClearPausedBatchResumeState();
            _viewModel.ResumeBatchEnabled = false;
            await DisplayAlert(
                AppText.Get("ResumeUnavailable"),
                AppText.Get("BatchFilesUnavailable"),
                AppText.Get("Ok"));
            return;
        }
        if (!TryTakeRemote(out var remote))
        {
            await DisplayAlert(
                AppText.Get("FreshPairingRequired"),
                AppText.Get("FreshPairingResumeMessage"),
                AppText.Get("Ok"));
            return;
        }

        await RunStableBatchSendAsync(remote, paths, transferId, isResume: true);
    }

    private async Task RunStableBatchSendAsync(
        PairingPayload remote,
        string[] paths,
        string transferId,
        bool isResume)
    {
        _batchCts?.Dispose();
        _batchCts = new CancellationTokenSource();
        _pauseBatch = false;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _viewModel.SetBatchTransferControls(sending: true, canResume: false);
            _viewModel.BatchTransferStatus = isResume
                ? AppText.Get("PreparingResumeChecksums")
                : AppText.Get("PreparingChecksums");
            var progress = new Progress<BatchProgress>(value =>
            {
                _viewModel.BatchTransferProgress = value.Fraction;
                var seconds = Math.Max(stopwatch.Elapsed.TotalSeconds, .001);
                var speed = value.CompletedBytes / seconds;
                var remaining = Math.Max(0, value.TotalBytes - value.CompletedBytes);
                var eta = speed <= 1
                    ? AppText.Get("Calculating")
                    : TimeSpan.FromSeconds(remaining / speed).ToString(@"hh\:mm\:ss");
                _viewModel.BatchTransferStatus = AppText.Format(
                    "BatchProgressFormat",
                    value.CompletedItems,
                    value.TotalItems,
                    FormatBytes(value.CompletedBytes),
                    FormatBytes(value.TotalBytes),
                    FormatBytes((long)speed),
                    eta,
                    value.CurrentFile);
            });

            var result = await _transfers.SendBatchAsync(
                remote,
                paths,
                transferId,
                progress,
                _batchCts.Token);
            foreach (var item in result.Completed)
                await _history.AddAsync("sent", remote.DeviceName, item.Entry.RelativePath, item.Entry.Length, "completed", true);
            foreach (var item in result.Skipped)
                await _history.AddAsync("sent", remote.DeviceName, item.Entry.RelativePath, item.Entry.Length, "not-selected", false);

            ClearPausedBatchResumeState();
            _viewModel.BatchTransferProgress = 1;
            _viewModel.BatchTransferStatus = AppText.Format(
                "BatchCompletedFormat",
                result.Completed.Count,
                result.Skipped.Count);
        }
        catch (OperationCanceledException)
        {
            if (_pauseBatch)
            {
                PreservePausedBatchResumeState(paths, transferId);
                _viewModel.BatchTransferStatus = AppText.Get("PausedResumeStatus");
                foreach (var path in _pausedBatchPaths.Where(File.Exists))
                {
                    var info = new FileInfo(path);
                    await _history.AddAsync("sent", remote.DeviceName, info.Name, info.Length, "paused", false);
                }
            }
            else
            {
                ClearPausedBatchResumeState();
                _viewModel.BatchTransferStatus = AppText.Get("BatchCancelledStatus");
            }
        }
        catch (Exception ex)
        {
            PreservePausedBatchResumeState(paths, transferId);
            _viewModel.BatchTransferStatus = _pausedBatchPaths.Length == 0
                ? AppText.Get("BatchFailedStatus")
                : AppText.Get("BatchFailedSafeResumeStatus");
            await DisplayAlert(AppText.Get("BatchTransferFailed"), ex.Message, AppText.Get("Ok"));
        }
        finally
        {
            stopwatch.Stop();
            _viewModel.SetBatchTransferControls(
                sending: false,
                canResume: _pausedBatchPaths.Length > 0 && !string.IsNullOrWhiteSpace(_pausedBatchTransferId));
        }
    }

    private void PauseBatchStableClicked(object? sender, EventArgs e)
    {
        _pauseBatch = true;
        _batchCts?.Cancel();
    }

    private void CancelBatchStableClicked(object? sender, EventArgs e)
    {
        _pauseBatch = false;
        ClearPausedBatchResumeState();
        _batchCts?.Cancel();
    }

    private void PreservePausedBatchResumeState(IEnumerable<string> paths, string transferId)
    {
        _pausedBatchPaths = paths.Where(SourceExists).Distinct(StringComparer.Ordinal).ToArray();
        _pausedBatchTransferId = _pausedBatchPaths.Length == 0 ? null : transferId;
    }

    private void ClearPausedBatchResumeState()
    {
        _pausedBatchPaths = Array.Empty<string>();
        _pausedBatchTransferId = null;
    }

    private static bool SourceExists(string path)
        => File.Exists(path) || Directory.Exists(path);
}
