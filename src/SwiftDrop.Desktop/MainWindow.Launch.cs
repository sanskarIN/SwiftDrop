using SwiftDrop.Core.Security;

namespace SwiftDrop.Desktop;

public sealed partial class MainWindow
{
    public void ApplyLaunchArguments(IEnumerable<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        foreach (var argument in arguments)
        {
            if (string.IsNullOrWhiteSpace(argument)) continue;
            var value = argument.Trim();
            if (!value.StartsWith("swiftdrop://pair", StringComparison.OrdinalIgnoreCase)) continue;

            try
            {
                var payload = _pairing.DecodePairingLink(value);
                _remotePairingLinkBox.Text = value;
                _remote = payload;
                ShowRemote(payload);
                SetStatus("Pairing link received from desktop launch");
            }
            catch (Exception ex) when (ex is InvalidDataException or ArgumentException or FormatException)
            {
                SetStatus($"Launch pairing link rejected: {ex.Message}");
            }

            break;
        }
    }
}
