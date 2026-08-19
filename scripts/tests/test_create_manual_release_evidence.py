import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
CREATE_SCRIPT = REPOSITORY_ROOT / "scripts" / "create_manual_release_evidence.py"
VALIDATE_SCRIPT = REPOSITORY_ROOT / "scripts" / "validate_manual_release_evidence.py"
VALID_COMMIT = "4566d9eb24247eb0a52a693a851822a1af9a02a8"


class CreateManualReleaseEvidenceTests(unittest.TestCase):
    def run_generator(
        self,
        output: Path,
        *,
        commit: str = VALID_COMMIT,
        version: str = "1.0.0-rc.1",
        created_utc: str = "2026-08-19T04:00:00Z",
        force: bool = False,
    ) -> subprocess.CompletedProcess[str]:
        command = [
            sys.executable,
            str(CREATE_SCRIPT),
            "--commit",
            commit,
            "--version",
            version,
            "--output",
            str(output),
            "--created-utc",
            created_utc,
        ]
        if force:
            command.append("--force")
        return subprocess.run(command, check=False, capture_output=True, text=True, encoding="utf-8")

    def test_generator_creates_structurally_valid_manifest(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "candidate.json"
            result = self.run_generator(output)
            validation = subprocess.run(
                [sys.executable, str(VALIDATE_SCRIPT), str(output)],
                check=False,
                capture_output=True,
                text=True,
                encoding="utf-8",
            )

            self.assertEqual(0, result.returncode, result.stderr)
            self.assertEqual(0, validation.returncode, validation.stderr)

    def test_generator_starts_every_case_as_not_run(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "candidate.json"
            result = self.run_generator(output)
            self.assertEqual(0, result.returncode, result.stderr)
            payload = json.loads(output.read_text(encoding="utf-8"))

            self.assertEqual(9, len(payload["groups"]))
            self.assertTrue(all(group["status"] == "not-run" for group in payload["groups"]))
            self.assertTrue(
                all(case["status"] == "not-run" for group in payload["groups"] for case in group["cases"])
            )

    def test_generator_rejects_placeholder_commit(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "candidate.json"
            result = self.run_generator(output, commit="0" * 40)

            self.assertEqual(1, result.returncode)
            self.assertIn("all-zero template placeholder", result.stderr)
            self.assertFalse(output.exists())

    def test_generator_rejects_noncanonical_commit(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "candidate.json"
            result = self.run_generator(output, commit=VALID_COMMIT.upper())

            self.assertEqual(1, result.returncode)
            self.assertIn("canonical lowercase 40-hex", result.stderr)
            self.assertFalse(output.exists())

    def test_generator_rejects_noncanonical_timestamp(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "candidate.json"
            result = self.run_generator(output, created_utc="2026-08-19 04:00:00Z")

            self.assertEqual(1, result.returncode)
            self.assertIn("canonical YYYY-MM-DDTHH:MM:SS", result.stderr)
            self.assertFalse(output.exists())

    def test_generator_refuses_to_overwrite_by_default(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "candidate.json"
            output.write_text("original", encoding="utf-8")
            result = self.run_generator(output)

            self.assertEqual(1, result.returncode)
            self.assertIn("use --force", result.stderr)
            self.assertEqual("original", output.read_text(encoding="utf-8"))

    def test_generator_force_replaces_regular_file(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "candidate.json"
            output.write_text("original", encoding="utf-8")
            result = self.run_generator(output, force=True)

            self.assertEqual(0, result.returncode, result.stderr)
            payload = json.loads(output.read_text(encoding="utf-8"))
            self.assertEqual(VALID_COMMIT, payload["candidate"]["commit"])

    def test_generator_creates_parent_directories(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "release" / "candidate" / "evidence.json"
            result = self.run_generator(output)

            self.assertEqual(0, result.returncode, result.stderr)
            self.assertTrue(output.is_file())


if __name__ == "__main__":
    unittest.main()
