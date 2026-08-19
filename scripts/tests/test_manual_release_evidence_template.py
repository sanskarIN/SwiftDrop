import subprocess
import sys
import unittest
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
SCRIPT = REPOSITORY_ROOT / "scripts" / "validate_manual_release_evidence.py"
TEMPLATE = REPOSITORY_ROOT / "docs" / "release" / "manual-release-evidence.template.json"


class ManualReleaseEvidenceTemplateTests(unittest.TestCase):
    def test_checked_in_template_is_valid(self) -> None:
        result = subprocess.run(
            [sys.executable, str(SCRIPT), str(TEMPLATE)],
            check=False,
            capture_output=True,
            text=True,
            encoding="utf-8",
        )

        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("manual release evidence valid", result.stdout)


if __name__ == "__main__":
    unittest.main()
