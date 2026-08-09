namespace SwiftDrop.App;

public partial class MainPage
{
    private int _identityRecoveryNoticeShown;

    public async Task ShowIdentityRecoveryNoticeAsync()
    {
        await _identity.InitializeAsync();
        if (!_identity.IdentityWasAutomaticallyRegenerated) return;
        if (Interlocked.Exchange(ref _identityRecoveryNoticeShown, 1) != 0) return;

        var reason = _identity.AutomaticRegenerationReason?.ToString() ?? "UnusableCertificate";
        await DisplayAlert(
            "Device identity refreshed",
            $"SwiftDrop created a new local device ID and certificate because the previous certificate could not be safely reused ({reason}). Other devices that trusted the previous certificate must pair with this device again. Received files, transfer history, and your list of devices you trust were not deleted.",
            "OK");
    }
}
