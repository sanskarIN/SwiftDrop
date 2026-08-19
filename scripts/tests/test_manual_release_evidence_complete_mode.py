import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
SCRIPT = REPOSITORY_ROOT / "scripts" / "validate_manual_release_evidence.py"
TEMPLATE = REPOSITORY_ROOT / "docs" / "release" / "manual-release-evidence.template.json"


class ManualReleaseEvidenceCompleteModeTests(unittest.TestCase):
    def run_complete(self, payload: object) -> subprocess.CompletedProcess[str]:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "evidence.json"
            path.write_text(json.dumps(payload), encoding="utf-8")
            return subprocess.run(
                [sys.executable, str(SCRIPT), "--require-complete", str(path)],
                check=False,
                capture_output=True,
                text=True,
                encoding="utf-8",
            )

    def load_template(self) -> dict[str, object]:
        return json.loads(TEMPLATE.read_text(encoding="utf-8"))

    def test_template_is_not_release_complete(self) -> None:
        result = subprocess.run(
            [sys.executable, str(SCRIPT), "--require-complete", str(TEMPLATE)],
            check=False,
            capture_output=True,
            text=True,
            encoding="utf-8",
        )

        self.assertEqual(1, result.returncode)
        self.assertIn("all-zero template placeholder", result.stderr)

    def test_all_passed_candidate_is_complete(self) -> None:
        payload = self.load_template()
        payload["candidate"]["commit"] = "f25f9ff65ddeb538f408bc9a1884ee141172e63c"
        payload["candidate"]["version"] = "1.0.0-rc.1"
        for group in payload["groups"]:
            group["status"] = "passed"
            for case in group["cases"]:
                case["status"] = "passed"
                case["executed_utc"] = "2026-08-19T03:00:00Z"
                case["environment"] = "representative signed-device validation environment"
                case["evidence"] = [f"evidence/{group['id']}/{case['id']}.txt"]

        result = self.run_complete(payload)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("valid (complete mode)", result.stdout)

    def test_incomplete_group_is_rejected_in_complete_mode(self) -> None:
        payload = self.load_template()
        payload["candidate"]["commit"] = "f25f9ff65ddeb538f408bc9a1884ee141172e63c"
        group = payload["groups"][0]
        case = group["cases"][0]
        case["status"] = "passed"
        case["executed_utc"] = "2026-08-19T03:00:00Z"
        case["environment"] = "signed Android device"
        case["evidence"] = ["evidence/android/install.txt"]
        group["status"] = "in-progress"

        result = self.run_complete(payload)
        self.assertEqual(1, result.returncode)
        self.assertIn("complete release-candidate mode", result.stderr)

    def test_sensitive_pairing_capability_is_rejected_from_evidence_reference(self) -> None:
        payload = self.load_template()
        case = payload["groups"][0]["cases"][0]
        case["status"] = "passed"
        case["executed_utc"] = "2026-08-19T03:00:00Z"
        case["environment"] = "signed Android device"
        case["evidence"] = ["swiftdrop://pair?p=secret"]
        payload["groups"][0]["status"] = "in-progress"

        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "evidence.json"
            path.write_text(json.dumps(payload), encoding="utf-8")
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(path)],
                check=False,
                capture_output=True,
                text=True,
                encoding="utf-8",
            )

        self.assertEqual(1, result.returncode)
        self.assertIn("pairing capability", result.stderr)


if __name__ == "__main__":
    unittest.main()
