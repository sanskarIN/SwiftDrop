import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPOSITORY_ROOT / "scripts"
sys.path.insert(0, str(SCRIPTS_ROOT))

import validate_repository_completion as completion  # noqa: E402


class RepositoryCompletionValidatorTests(unittest.TestCase):
    def test_current_repository_passes_completion_contract(self) -> None:
        result = subprocess.run(
            [sys.executable, str(SCRIPTS_ROOT / "validate_repository_completion.py"), str(REPOSITORY_ROOT)],
            check=False,
            capture_output=True,
            text=True,
            encoding="utf-8",
        )

        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("repository completion contract valid", result.stdout)

    def test_unfinished_marker_in_production_source_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / "src" / "Example.cs"
            source.parent.mkdir(parents=True)
            source.write_text("// TODO: finish this\n", encoding="utf-8")

            errors = completion.validate_no_unfinished_markers(root)

            self.assertTrue(any("TODO" in error and "Example.cs" in error for error in errors))

    def test_missing_release_critical_trigger_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            workflow = root / ".github" / "workflows" / "release-readiness.yml"
            workflow.parent.mkdir(parents=True)
            workflow.write_text("name: Release readiness\n", encoding="utf-8")

            errors = completion.validate_release_readiness_triggers(root)

            self.assertTrue(any("validate_manual_release_evidence.py" in error for error in errors))
            self.assertTrue(any("validate_repository_completion.py" in error for error in errors))

    def test_missing_final_documentation_link_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            index = root / "docs" / "README.md"
            index.parent.mkdir(parents=True)
            index.write_text("# Docs\n", encoding="utf-8")

            errors = completion.validate_documentation_index(root)

            self.assertTrue(any("continuation-status-2026-08-19.md" in error for error in errors))

    def test_missing_required_repository_file_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            errors = completion.validate_required_paths(Path(directory))

            self.assertTrue(any("README.md" in error for error in errors))
            self.assertTrue(any("SECURITY.md" in error for error in errors))

    def test_invalid_manual_release_template_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            template = root / "docs" / "release" / "manual-release-evidence.template.json"
            template.parent.mkdir(parents=True)
            template.write_text("{}\n", encoding="utf-8")

            errors = completion.validate_release_template(root)

            self.assertTrue(any("template is invalid" in error for error in errors))


if __name__ == "__main__":
    unittest.main()
