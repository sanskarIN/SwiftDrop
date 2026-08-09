namespace SwiftDrop.App.Services;

public sealed record IncomingPairingRequest(
    string SenderDeviceId,
    string SenderDeviceName,
    string SenderCertificateFingerprint);
