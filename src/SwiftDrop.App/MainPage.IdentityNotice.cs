using SwiftDrop.App.Services;

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
            AppText.Get("DeviceIdentityRefreshed"),
            AppText.Format("DeviceIdentityRefreshedFormat", reason),
            AppText.Get("Ok"));
    }
}
