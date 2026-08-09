using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;

namespace SwiftDrop.App.Services;

public sealed record IncomingTransferPreview(
    string SenderDeviceId,
    string SenderDeviceName,
    string SenderCertificateFingerprint,
    FileManifestEntry Entry,
    FileRiskLevel RiskLevel);
