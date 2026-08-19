import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
SCRIPT = REPOSITORY_ROOT / "scripts" / "validate_manual_release_evidence.py"
TEMPLATE = REPOSITORY_ROOT / "docs" / "release" / "manual-release-evidence.template.json"


class ManualReleaseEvidenceTimestampTests(unittest.TestCase):
    def run_validator(self, timestamp: str) -> subprocess.CompletedProcess[str]:
        payload = json.loads(TEMPLATE.read_text(encoding="utf-8"))
        payload["candidate"]["created_utc"] = timestamp
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "evidence.json"
            path.write_text(json.dumps(payload), encoding="utf-8")
            return subprocess.run(
                [sys.executable, str(SCRIPT), str(path)],
                check=False,
                capture_output=True,
                text=True,
                encoding="utf-8",
            )

    def test_space_separator_alias_is_rejected(self) -> None:
        result = self.run_validator("2026-08-19 03:15:00Z")
        self.assertEqual(1, result.returncode)
        self.assertIn("canonical YYYY-MM-DDTHH:MM:SS", result.stderr)

    def test_explicit_offset_alias_is_rejected(self) -> None:
        result = self.run_validator("2026-08-19T03:15:00+00:00")
        self.assertEqual(1, result.returncode)
        self.assertIn("canonical YYYY-MM-DDTHH:MM:SS", result.stderr)

    def test_fractional_utc_timestamp_is_accepted(self) -> None:
        result = self.run_validator("2026-08-19T03:15:00.123456Z")
        self.assertEqual(0, result.returncode, result.stderr)


if __name__ == "__main__":
    unittest.main()
