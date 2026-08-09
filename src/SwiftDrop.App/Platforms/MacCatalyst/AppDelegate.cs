using Foundation;
using SwiftDrop.App.Services;
using UIKit;

namespace SwiftDrop.App;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
    {
        var link = url.AbsoluteString;
        if (!string.IsNullOrWhiteSpace(link) && link.StartsWith("swiftdrop://pair", StringComparison.OrdinalIgnoreCase))
        {
            ExternalInputInbox.SetPairingLink(link);
            return true;
        }
        return base.OpenUrl(application, url, options);
    }
}
