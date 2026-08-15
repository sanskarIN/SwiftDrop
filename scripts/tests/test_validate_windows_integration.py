import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).resolve().parents[1] / "validate_windows_integration.py"
CLSID = "A630B8B4-6522-4EA0-9BBE-A2C7C40BB839"


class WindowsIntegrationValidatorTests(unittest.TestCase):
    def make_fixture(self, root: Path) -> None:
        manifest = root / "src/SwiftDrop.App/Platforms/Windows/Package.appxmanifest"
        service = root / "src/SwiftDrop.App/Services/TransferNotificationService.cs"
        strings = root / "src/SwiftDrop.App/Resources/Strings/PlatformRuntimeStrings.resx"
        hindi = root / "src/SwiftDrop.App/Resources/Strings/PlatformRuntimeStrings.hi.resx"
        for path in (manifest, service, strings, hindi):
            path.parent.mkdir(parents=True, exist_ok=True)

        manifest.write_text(
            f'''<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
         xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
         xmlns:com="http://schemas.microsoft.com/appx/manifest/com/windows10"
         xmlns:desktop="http://schemas.microsoft.com/appx/manifest/desktop/windows10"
         IgnorableNamespaces="uap com desktop">
  <Applications>
    <Application Id="App" Executable="$targetnametoken$.exe" EntryPoint="$targetentrypoint$">
      <Extensions>
        <uap:Extension Category="windows.protocol"><uap:Protocol Name="swiftdrop" /></uap:Extension>
        <desktop:Extension Category="windows.toastNotificationActivation">
          <desktop:ToastNotificationActivation ToastActivatorCLSID="{CLSID}" />
        </desktop:Extension>
        <com:Extension Category="windows.comServer">
          <com:ComServer>
            <com:ExeServer Executable="$targetnametoken$.exe" Arguments="----AppNotificationActivated:" DisplayName="SwiftDrop">
              <com:Class Id="{CLSID}" />
            </com:ExeServer>
          </com:ComServer>
        </com:Extension>
      </Extensions>
    </Application>
  </Applications>
  <Capabilities><Capability Name="privateNetworkClientServer" /></Capabilities>
</Package>
''',
            encoding="utf-8",
        )
        service.write_text(
            '''var manager = AppNotificationManager.Default;
manager.Register();
var text = AppText.Get(success ? "TransferCompletedNotification" : "TransferFailedNotification");
_windowsManager!.Show(notification);
''',
            encoding="utf-8",
        )
        catalog = '''<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="TransferCompletedNotification"><value>Transfer completed.</value></data>
  <data name="TransferFailedNotification"><value>Transfer failed. Open SwiftDrop.</value></data>
</root>
'''
        strings.write_text(catalog, encoding="utf-8")
        hindi.write_text(catalog, encoding="utf-8")

    def run_validator(self, root: Path) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [sys.executable, str(SCRIPT), "--root", str(root)],
            check=False,
            capture_output=True,
            text=True,
            encoding="utf-8",
        )

    def test_valid_packaged_notification_contract_passes(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.make_fixture(root)
            result = self.run_validator(root)
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn("internally consistent", result.stdout)

    def test_mismatched_notification_clsid_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.make_fixture(root)
            manifest = root / "src/SwiftDrop.App/Platforms/Windows/Package.appxmanifest"
            text = manifest.read_text(encoding="utf-8").replace(
                f'<com:Class Id="{CLSID}" />',
                '<com:Class Id="E606CE24-9452-4DFE-8A24-8D938727FE86" />',
            )
            manifest.write_text(text, encoding="utf-8")
            result = self.run_validator(root)
            self.assertEqual(1, result.returncode)
            self.assertIn("must match", result.stderr)

    def test_internet_client_capability_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.make_fixture(root)
            manifest = root / "src/SwiftDrop.App/Platforms/Windows/Package.appxmanifest"
            text = manifest.read_text(encoding="utf-8").replace(
                '<Capability Name="privateNetworkClientServer" />',
                '<Capability Name="privateNetworkClientServer" /><Capability Name="internetClient" />',
            )
            manifest.write_text(text, encoding="utf-8")
            result = self.run_validator(root)
            self.assertEqual(1, result.returncode)
            self.assertIn("must not add internetClient", result.stderr)

    def test_notification_placeholders_are_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.make_fixture(root)
            catalog = root / "src/SwiftDrop.App/Resources/Strings/PlatformRuntimeStrings.resx"
            text = catalog.read_text(encoding="utf-8").replace(
                "Transfer completed.",
                "Transfer completed: {0}",
            )
            catalog.write_text(text, encoding="utf-8")
            result = self.run_validator(root)
            self.assertEqual(1, result.returncode)
            self.assertIn("placeholder-free", result.stderr)


if __name__ == "__main__":
    unittest.main()
