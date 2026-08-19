import json
import re
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
CREATE_SCRIPT = REPOSITORY_ROOT / "scripts" / "create_manual_release_evidence.py"
VALID_COMMIT = "4566d9eb24247eb0a52a693a851822a1af9a02a8"
UTC_RE = re.compile(r"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$")


class CreateManualReleaseEvidenceDefaultTests(unittest.TestCase):
    def test_default_created_timestamp_is_canonical_utc(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "candidate.json"
            result = subprocess.run(
                [
                    sys.executable,
                    str(CREATE_SCRIPT),
                    "--commit",
                    VALID_COMMIT,
                    "--version",
                    "1.0.0-rc.1",
                    "--output",
                    str(output),
                ],
                check=False,
                capture_output=True,
                text=True,
                encoding="utf-8",
            )

            self.assertEqual(0, result.returncode, result.stderr)
            payload = json.loads(output.read_text(encoding="utf-8"))
            self.assertRegex(payload["candidate"]["created_utc"], UTC_RE)

    def test_force_does_not_replace_directory(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "candidate.json"
            output.mkdir()
            result = subprocess.run(
                [
                    sys.executable,
                    str(CREATE_SCRIPT),
                    "--commit",
                    VALID_COMMIT,
                    "--version",
                    "1.0.0-rc.1",
                    "--output",
                    str(output),
                    "--created-utc",
                    "2026-08-19T04:30:00Z",
                    "--force",
                ],
                check=False,
                capture_output=True,
                text=True,
                encoding="utf-8",
            )

            self.assertEqual(1, result.returncode)
            self.assertIn("not a regular file", result.stderr)
            self.assertTrue(output.is_dir())


if __name__ == "__main__":
    unittest.main()
