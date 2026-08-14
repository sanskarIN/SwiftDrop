import hashlib
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).resolve().parents[1] / "create_dependency_evidence_manifest.py"


class DependencyEvidenceManifestTests(unittest.TestCase):
    def run_manifest(self, root: Path, output: Path) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [sys.executable, str(SCRIPT), str(root), str(output)],
            check=False,
            capture_output=True,
            text=True,
            encoding="utf-8",
        )

    def test_manifest_is_sorted_and_hashes_report_bytes(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "evidence"
            nested = root / "nested"
            nested.mkdir(parents=True)
            first = root / "b.json"
            second = nested / "a.json"
            first.write_bytes(b'{"b":2}\n')
            second.write_bytes(b'{"a":1}\n')
            output = root / "manifest.json"

            result = self.run_manifest(root, output)

            self.assertEqual(0, result.returncode, result.stderr)
            payload = json.loads(output.read_text(encoding="utf-8"))
            self.assertEqual(1, payload["schemaVersion"])
            self.assertEqual(2, payload["fileCount"])
            self.assertEqual(["b.json", "nested/a.json"], [item["path"] for item in payload["files"]])
            self.assertEqual(hashlib.sha256(first.read_bytes()).hexdigest(), payload["files"][0]["sha256"])
            self.assertEqual(hashlib.sha256(second.read_bytes()).hexdigest(), payload["files"][1]["sha256"])

    def test_existing_output_is_excluded_from_its_own_manifest(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "packages.json").write_text("{}\n", encoding="utf-8")
            output = root / "manifest.json"
            output.write_text('{"stale":true}\n', encoding="utf-8")

            result = self.run_manifest(root, output)

            self.assertEqual(0, result.returncode, result.stderr)
            payload = json.loads(output.read_text(encoding="utf-8"))
            self.assertEqual(1, payload["fileCount"])
            self.assertEqual("packages.json", payload["files"][0]["path"])

    def test_empty_evidence_directory_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            output = root / "manifest.json"

            result = self.run_manifest(root, output)

            self.assertEqual(2, result.returncode)
            self.assertIn("No dependency-evidence JSON files found", result.stderr)
            self.assertFalse(output.exists())

    def test_output_outside_root_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            base = Path(directory)
            root = base / "evidence"
            root.mkdir()
            (root / "packages.json").write_text("{}\n", encoding="utf-8")
            output = base / "manifest.json"

            result = self.run_manifest(root, output)

            self.assertEqual(2, result.returncode)
            self.assertIn("must remain beneath evidence root", result.stderr)
            self.assertFalse(output.exists())


if __name__ == "__main__":
    unittest.main()
