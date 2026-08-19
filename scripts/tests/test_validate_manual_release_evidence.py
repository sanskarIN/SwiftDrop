import copy
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).resolve().parents[1] / "validate_manual_release_evidence.py"

REQUIRED_CASES = {
    "android": [
        "signed-install-upgrade",
        "share-provider-metadata",
        "foreground-background",
        "multicast-discovery",
    ],
    "windows": [
        "signed-msix-install-upgrade",
        "protocol-activation",
        "firewall-network",
        "folder-picker-drop",
    ],
    "ios": [
        "signed-device-build",
        "share-extension",
        "app-group-handoff",
        "item-provider",
    ],
    "maccatalyst": ["signed-notarized-build", "sandbox-network", "native-drop"],
    "cross-device": [
        "pairing-methods",
        "file-folder-text",
        "pause-cancel-resume",
        "network-switching",
        "low-storage",
    ],
    "filesystem": ["symlink-reparse", "collision-pressure", "destination-mutation"],
    "accessibility-localization": [
        "screen-readers",
        "large-text-high-contrast",
        "hindi-layout",
    ],
    "dependency-license": ["exact-graph-audit", "licenses-notices", "provenance"],
    "store": ["privacy-declarations", "metadata-screenshots", "submission-signing"],
}


def make_case(case_id: str) -> dict[str, object]:
    return {
        "id": case_id,
        "status": "not-run",
        "executed_utc": None,
        "environment": "",
        "evidence": [],
        "notes": "",
    }


def valid_payload() -> dict[str, object]:
    return {
        "schema_version": 1,
        "candidate": {
            "commit": "0123456789abcdef0123456789abcdef01234567",
            "version": "1.0.0-rc.1",
            "created_utc": "2026-08-19T02:00:00Z",
        },
        "groups": [
            {
                "id": group_id,
                "status": "not-run",
                "cases": [make_case(case_id) for case_id in case_ids],
            }
            for group_id, case_ids in REQUIRED_CASES.items()
        ],
    }


class ManualReleaseEvidenceValidatorTests(unittest.TestCase):
    def run_validator(self, payload: object) -> subprocess.CompletedProcess[str]:
        with tempfile.TemporaryDirectory() as directory:
            report = Path(directory) / "manual-release-evidence.json"
            report.write_text(json.dumps(payload), encoding="utf-8")
            return subprocess.run(
                [sys.executable, str(SCRIPT), str(report)],
                check=False,
                capture_output=True,
                text=True,
                encoding="utf-8",
            )

    def test_complete_not_run_template_is_valid(self) -> None:
        result = self.run_validator(valid_payload())
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("manual release evidence valid", result.stdout)

    def test_passed_case_requires_timestamp_environment_and_evidence(self) -> None:
        payload = valid_payload()
        case = payload["groups"][0]["cases"][0]
        case["status"] = "passed"
        payload["groups"][0]["status"] = "in-progress"

        result = self.run_validator(payload)
        self.assertEqual(1, result.returncode)
        self.assertIn("executed_utc", result.stderr)

    def test_all_passed_cases_require_passed_group_status(self) -> None:
        payload = valid_payload()
        group = payload["groups"][0]
        for case in group["cases"]:
            case["status"] = "passed"
            case["executed_utc"] = "2026-08-19T02:15:00Z"
            case["environment"] = "Pixel physical device / signed candidate"
            case["evidence"] = [f"evidence/{case['id']}.txt"]
        group["status"] = "in-progress"

        result = self.run_validator(payload)
        self.assertEqual(1, result.returncode)
        self.assertIn("must be 'passed'", result.stderr)

    def test_missing_required_group_is_rejected(self) -> None:
        payload = valid_payload()
        payload["groups"].pop()

        result = self.run_validator(payload)
        self.assertEqual(1, result.returncode)
        self.assertIn("exactly 9 required groups", result.stderr)

    def test_duplicate_case_is_rejected(self) -> None:
        payload = valid_payload()
        group = payload["groups"][0]
        group["cases"][1]["id"] = group["cases"][0]["id"]

        result = self.run_validator(payload)
        self.assertEqual(1, result.returncode)
        self.assertIn("duplicates an earlier case", result.stderr)

    def test_unknown_fields_are_rejected(self) -> None:
        payload = valid_payload()
        payload["candidate"]["branch"] = "main"

        result = self.run_validator(payload)
        self.assertEqual(1, result.returncode)
        self.assertIn("unknown field", result.stderr)

    def test_noncanonical_commit_is_rejected(self) -> None:
        payload = valid_payload()
        payload["candidate"]["commit"] = "ABCDEF0123456789ABCDEF0123456789ABCDEF01"

        result = self.run_validator(payload)
        self.assertEqual(1, result.returncode)
        self.assertIn("canonical lowercase 40-hex", result.stderr)

    def test_blocked_case_requires_notes(self) -> None:
        payload = valid_payload()
        case = payload["groups"][0]["cases"][0]
        case["status"] = "blocked"
        payload["groups"][0]["status"] = "blocked"

        result = self.run_validator(payload)
        self.assertEqual(1, result.returncode)
        self.assertIn("notes", result.stderr)

    def test_pairing_capability_is_rejected_from_notes(self) -> None:
        payload = valid_payload()
        case = payload["groups"][0]["cases"][0]
        case["status"] = "blocked"
        case["notes"] = "Captured swiftdrop://pair?p=secret during debugging"
        payload["groups"][0]["status"] = "blocked"

        result = self.run_validator(payload)
        self.assertEqual(1, result.returncode)
        self.assertIn("pairing capability", result.stderr)

    def test_valid_mixed_group_is_in_progress(self) -> None:
        payload = valid_payload()
        group = payload["groups"][0]
        case = group["cases"][0]
        case["status"] = "passed"
        case["executed_utc"] = "2026-08-19T02:15:00Z"
        case["environment"] = "Pixel physical device / signed candidate"
        case["evidence"] = ["evidence/android-install.txt"]
        group["status"] = "in-progress"

        result = self.run_validator(payload)
        self.assertEqual(0, result.returncode, result.stderr)


if __name__ == "__main__":
    unittest.main()
