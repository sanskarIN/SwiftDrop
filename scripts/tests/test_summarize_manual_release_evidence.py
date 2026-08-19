import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
CREATE_SCRIPT = REPOSITORY_ROOT / "scripts" / "create_manual_release_evidence.py"
SUMMARY_SCRIPT = REPOSITORY_ROOT / "scripts" / "summarize_manual_release_evidence.py"
VALID_COMMIT = "4566d9eb24247eb0a52a693a851822a1af9a02a8"


class SummarizeManualReleaseEvidenceTests(unittest.TestCase):
    def create_manifest(self, output: Path) -> dict:
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
                "2026-08-19T08:00:00Z",
            ],
            check=False,
            capture_output=True,
            text=True,
            encoding="utf-8",
        )
        self.assertEqual(0, result.returncode, result.stderr)
        return json.loads(output.read_text(encoding="utf-8"))

    def run_summary(
        self,
        path: Path,
        *,
        as_json: bool = False,
        remaining_only: bool = False,
    ) -> subprocess.CompletedProcess[str]:
        command = [sys.executable, str(SUMMARY_SCRIPT), str(path)]
        if as_json:
            command.append("--json")
        if remaining_only:
            command.append("--remaining-only")
        return subprocess.run(command, check=False, capture_output=True, text=True, encoding="utf-8")

    def test_text_summary_reports_initial_progress(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "evidence.json"
            self.create_manifest(path)

            result = self.run_summary(path)

            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn("cases: 0/32 passed; 32 remaining", result.stdout)
            self.assertIn("complete: no", result.stdout)
            self.assertIn("android: not-run", result.stdout)

    def test_remaining_only_lists_each_unpassed_required_case(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "evidence.json"
            self.create_manifest(path)

            result = self.run_summary(path, remaining_only=True)

            self.assertEqual(0, result.returncode, result.stderr)
            lines = result.stdout.strip().splitlines()
            self.assertEqual(32, len(lines))
            self.assertIn("android/signed-install-upgrade: not-run", lines)
            self.assertIn("store/submission-signing: not-run", lines)

    def test_json_summary_counts_blocked_case(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "evidence.json"
            payload = self.create_manifest(path)
            android = payload["groups"][0]
            android["cases"][0]["status"] = "blocked"
            android["cases"][0]["notes"] = "Physical test device unavailable."
            android["status"] = "blocked"
            path.write_text(json.dumps(payload), encoding="utf-8")

            result = self.run_summary(path, as_json=True)
            self.assertEqual(0, result.returncode, result.stderr)
            summary = json.loads(result.stdout)

            self.assertEqual(32, summary["total_cases"])
            self.assertEqual(1, summary["case_counts"]["blocked"])
            self.assertEqual(31, summary["case_counts"]["not-run"])
            self.assertEqual(1, summary["group_counts"]["blocked"])
            self.assertFalse(summary["complete"])
            self.assertEqual(32, len(summary["remaining"]))
            self.assertEqual(
                {
                    "group": "android",
                    "case": "signed-install-upgrade",
                    "status": "blocked",
                },
                summary["remaining"][0],
            )

    def test_json_summary_reports_complete_when_every_case_passes(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "evidence.json"
            payload = self.create_manifest(path)
            for group in payload["groups"]:
                group["status"] = "passed"
                for case in group["cases"]:
                    case["status"] = "passed"
                    case["executed_utc"] = "2026-08-19T09:00:00Z"
                    case["environment"] = "Representative signed release-candidate environment"
                    case["evidence"] = [f"evidence/{group['id']}/{case['id']}.txt"]
            path.write_text(json.dumps(payload), encoding="utf-8")

            result = self.run_summary(path, as_json=True)
            self.assertEqual(0, result.returncode, result.stderr)
            summary = json.loads(result.stdout)

            self.assertTrue(summary["complete"])
            self.assertEqual(32, summary["passed_cases"])
            self.assertEqual(0, summary["remaining_cases"])
            self.assertEqual(9, summary["group_counts"]["passed"])
            self.assertEqual([], summary["remaining"])

            remaining = self.run_summary(path, remaining_only=True)
            self.assertEqual(0, remaining.returncode, remaining.stderr)
            self.assertEqual("all required manual release-evidence cases are passed", remaining.stdout.strip())

    def test_summary_rejects_invalid_evidence_before_reporting(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "evidence.json"
            path.write_text('{"schema_version": 1}', encoding="utf-8")

            result = self.run_summary(path)

            self.assertEqual(1, result.returncode)
            self.assertIn("could not summarize manual release evidence", result.stderr)
            self.assertEqual("", result.stdout)


if __name__ == "__main__":
    unittest.main()
