import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).resolve().parents[1] / "validate_version_alignment.py"


class VersionAlignmentValidatorTests(unittest.TestCase):
    def make_fixture(
        self,
        root: Path,
        *,
        app_version: str = "2.5.18",
        app_build: str = "20518",
        share_version: str = "2.5.18",
        share_build: str = "20518",
        desktop_version: str = "2.5.18",
        windows_version: str = "2.5.18.0",
    ) -> None:
        app = root / "src/SwiftDrop.App/SwiftDrop.App.csproj"
        share = root / "src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj"
        desktop = root / "src/SwiftDrop.Desktop/SwiftDrop.Desktop.csproj"
        windows = root / "src/SwiftDrop.App/Platforms/Windows/Package.appxmanifest"
        for path in (app, share, desktop, windows):
            path.parent.mkdir(parents=True, exist_ok=True)

        app.write_text(
            f'''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<ApplicationDisplayVersion>{app_version}</ApplicationDisplayVersion>
<ApplicationVersion>{app_build}</ApplicationVersion>
</PropertyGroup></Project>''',
            encoding="utf-8",
        )
        share.write_text(
            f'''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<ApplicationDisplayVersion>{share_version}</ApplicationDisplayVersion>
<ApplicationVersion>{share_build}</ApplicationVersion>
</PropertyGroup></Project>''',
            encoding="utf-8",
        )
        desktop.write_text(
            f'''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<Version>{desktop_version}</Version>
</PropertyGroup></Project>''',
            encoding="utf-8",
        )
        windows.write_text(
            f'''<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10">
  <Identity Name="Sanskar.SwiftDrop" Publisher="CN=SwiftDrop" Version="{windows_version}" />
</Package>''',
            encoding="utf-8",
        )

    def run_validator(self, root: Path, *extra: str) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [sys.executable, str(SCRIPT), "--root", str(root), *extra],
            check=False,
            capture_output=True,
            text=True,
            encoding="utf-8",
        )

    def test_aligned_2518_release_passes(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.make_fixture(root)
            result = self.run_validator(root, "--expected-version", "2.5.18", "--expected-build", "20518")
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn("display=2.5.18", result.stdout)

    def test_share_display_mismatch_fails(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.make_fixture(root, share_version="2.5.17")
            result = self.run_validator(root)
            self.assertNotEqual(0, result.returncode)
            self.assertIn("Share Extension display versions must match", result.stderr)

    def test_desktop_version_mismatch_fails(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.make_fixture(root, desktop_version="2.5.17")
            result = self.run_validator(root)
            self.assertNotEqual(0, result.returncode)
            self.assertIn("desktop host versions must match", result.stderr)

    def test_build_code_convention_mismatch_fails(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.make_fixture(root, app_build="20519", share_build="20519")
            result = self.run_validator(root)
            self.assertNotEqual(0, result.returncode)
            self.assertIn("major*10000 + minor*100 + patch", result.stderr)

    def test_windows_package_mismatch_fails(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.make_fixture(root, windows_version="2.5.17.0")
            result = self.run_validator(root)
            self.assertNotEqual(0, result.returncode)
            self.assertIn("Windows package version must be 2.5.18.0", result.stderr)

    def test_expected_release_version_mismatch_fails(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.make_fixture(root)
            result = self.run_validator(root, "--expected-version", "2.5.17")
            self.assertNotEqual(0, result.returncode)
            self.assertIn("Expected release version 2.5.17", result.stderr)


if __name__ == "__main__":
    unittest.main()
