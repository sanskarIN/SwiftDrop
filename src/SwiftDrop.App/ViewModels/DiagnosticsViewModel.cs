using System.Collections.ObjectModel;
using SwiftDrop.App.Services;
using SwiftDrop.Core.Diagnostics;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;

namespace SwiftDrop.App.ViewModels;

public sealed class DiagnosticsViewModel : ObservableObject
{
    private readonly NetworkDiagnosticsService _diagnostics;
    private readonly NearbyDiscoveryService _discovery;
    private readonly DiagnosticLogService _log;
    private readonly TransferSelfTestService _selfTests;
    private readonly AppSettingsService _settings;
    private string _protocolStatus = string.Empty;
    private string _mdnsStatus = string.Empty;
    private string _udpStatus = string.Empty;
    private string _selfTestResult = string.Empty;
    private bool _developerOptionsEnabled;

    public DiagnosticsViewModel(
        NetworkDiagnosticsService diagnostics,
        NearbyDiscoveryService discovery,
        DiagnosticLogService log,
        TransferSelfTestService selfTests,
        AppSettingsService settings)
    {
        _diagnostics = diagnostics;
        _discovery = discovery;
        _log = log;
        _selfTests = selfTests;
        _settings = settings;
    }

    public ObservableCollection<NetworkDiagnostic> Diagnostics { get; } = new();
    public ObservableCollection<DiagnosticEvent> Events { get; } = new();

    public string ProtocolStatus { get => _protocolStatus; private set => SetProperty(ref _protocolStatus, value); }
    public string MdnsStatus { get => _mdnsStatus; private set => SetProperty(ref _mdnsStatus, value); }
    public string UdpStatus { get => _udpStatus; private set => SetProperty(ref _udpStatus, value); }
    public string SelfTestResult { get => _selfTestResult; private set => SetProperty(ref _selfTestResult, value); }
    public bool DeveloperOptionsEnabled { get => _developerOptionsEnabled; private set => SetProperty(ref _developerOptionsEnabled, value); }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        ProtocolStatus = AppText.Format("ProtocolVersionFormat", ProtocolConstants.CurrentVersion);
        DeveloperOptionsEnabled = _settings.Load().DeveloperOptionsEnabled;
        Diagnostics.Clear();
        try
        {
            await _discovery.StartAsync(ct);
            var running = AppText.Get("StatusRunning");
            var unavailable = AppText.Get("StatusUnavailable");
            MdnsStatus = AppText.Format(
                "MdnsDiscoveryStatusFormat",
                _discovery.IsMdnsRunning ? running : unavailable);
            UdpStatus = AppText.Format(
                "UdpFallbackStatusFormat",
                _discovery.IsUdpRunning ? running : unavailable);
            var current = _diagnostics.InspectLocalNetwork();
            foreach (var item in current)
            {
                Diagnostics.Add(item);
                await _log.RecordAsync(item.Severity.ToString(), item.Code, item.Title, ct);
            }
            await _log.RecordAsync(
                "Info",
                "discovery.status",
                $"mDNS={_discovery.IsMdnsRunning}; UDP={_discovery.IsUdpRunning}; protocol={ProtocolConstants.CurrentVersion}",
                ct);
        }
        catch (Exception ex)
        {
            var unavailable = AppText.Get("StatusUnavailable");
            MdnsStatus = AppText.Format("MdnsDiscoveryStatusFormat", unavailable);
            UdpStatus = AppText.Format("UdpFallbackStatusFormat", unavailable);
            Diagnostics.Add(new NetworkDiagnostic(
                "diagnostics.discovery_error",
                AppText.Get("AutomaticDiscoveryUnavailableTitle"),
                AppText.Get("AutomaticDiscoveryUnavailableMessage"),
                DiagnosticSeverity.Warning));
            await _log.RecordAsync("Warning", "diagnostics.discovery_error", $"Discovery startup failed with {ex.GetType().Name}.", ct);
        }
        await RefreshEventsAsync(ct);
    }

    public async Task<string> ExportAsync(CancellationToken ct = default)
        => await _log.ExportSafeTextAsync(ct);

    public async Task ClearEventsAsync(CancellationToken ct = default)
    {
        await _log.ClearAsync(ct);
        Events.Clear();
    }

    public Task RunRoundTripAsync(CancellationToken ct = default)
        => RunSelfTestAsync(token => _selfTests.RunSuccessfulRoundTripAsync(token), ct);

    public Task RunChecksumMismatchAsync(CancellationToken ct = default)
        => RunSelfTestAsync(token => _selfTests.RunChecksumMismatchAsync(token), ct);

    public Task RunInterruptedReceiveAsync(CancellationToken ct = default)
        => RunSelfTestAsync(token => _selfTests.RunInterruptedReceiveAsync(token), ct);

    private async Task RunSelfTestAsync(
        Func<CancellationToken, Task<SelfTestResult>> action,
        CancellationToken ct)
    {
        DeveloperOptionsEnabled = _settings.Load().DeveloperOptionsEnabled;
        if (!DeveloperOptionsEnabled)
            throw new InvalidOperationException(AppText.Get("DeveloperOptionsDisabled"));

        SelfTestResult = AppText.Get("RunningSyntheticSelfTest");
        try
        {
            var result = await action(ct);
            var resultLabel = AppText.Get(result.Passed ? "SelfTestPass" : "SelfTestFail");
            SelfTestResult = AppText.Format("SelfTestResultFormat", resultLabel, result.Message);
            await _log.RecordAsync(result.Passed ? "Info" : "Error", $"selftest.{result.Code}", result.Message, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            SelfTestResult = AppText.Format("SelfTestFailedFormat", ex.GetType().Name);
            await _log.RecordAsync("Error", "selftest.exception", $"Self-test failed with {ex.GetType().Name}.", ct);
        }
        await RefreshEventsAsync(ct);
    }

    private async Task RefreshEventsAsync(CancellationToken ct)
    {
        var events = await _log.GetRecentAsync(200, ct);
        Events.Clear();
        foreach (var item in events) Events.Add(item);
    }
}
