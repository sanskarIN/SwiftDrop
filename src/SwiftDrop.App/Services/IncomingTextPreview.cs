namespace SwiftDrop.App.Services;

public enum IncomingTextDecision
{
    Reject,
    Accept,
    AcceptAndCopy
}

public sealed record IncomingTextPreview(
    string SenderDeviceId,
    string SenderDeviceName,
    string SenderCertificateFingerprint,
    string Text,
    DateTimeOffset ExpiresUtc)
{
    public int CharacterCount => Text.Length;
}
