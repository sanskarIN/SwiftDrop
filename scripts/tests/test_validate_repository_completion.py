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

    def test_production_source_integrity_rejects_unfinished_and_non_utf8_source(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source_root = root / "src"
            source_root.mkdir(parents=True)
            (source_root / "Todo.cs").write_text("// TODO: finish this\n", encoding="utf-8")
            (source_root / "Invalid.cs").write_bytes(b"\xff\xfe\x00\x00")

            errors = completion.validate_no_unfinished_markers(root)

            self.assertTrue(any("TODO" in error and "Todo.cs" in error for error in errors))
            self.assertTrue(any("could not inspect production source" in error and "Invalid.cs" in error for error in errors))

    def test_release_trigger_and_portable_verifier_gaps_are_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            workflow = root / ".github" / "workflows" / "release-readiness.yml"
            workflow.parent.mkdir(parents=True)
            workflow.write_text("name: Release readiness\n", encoding="utf-8")

            for relative in (".github/workflows/ci.yml", "scripts/verify-core.sh", "scripts/verify-core.ps1"):
                path = root / relative
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text("missing completion call\n", encoding="utf-8")

            trigger_errors = completion.validate_release_readiness_triggers(root)
            integration_errors = completion.validate_portable_verifier_integration(root)

            self.assertTrue(any("validate_manual_release_evidence.py" in error for error in trigger_errors))
            self.assertTrue(any("summarize_manual_release_evidence.py" in error for error in trigger_errors))
            self.assertTrue(any("validate_repository_completion.py" in error for error in trigger_errors))
            self.assertEqual(3, len(integration_errors))

    def test_required_surface_and_final_documentation_links_are_enforced(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for relative in completion.REQUIRED_PATHS + completion.REQUIRED_PROJECTS:
                path = root / relative
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text("complete\n", encoding="utf-8")

            (root / "SECURITY.md").unlink()
            (root / "NOTICE").write_text("", encoding="utf-8")
            (root / "src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj").unlink()
            index = root / "docs/README.md"
            index.write_text("# Docs\n", encoding="utf-8")

            required_errors = completion.validate_required_paths(root)
            index_errors = completion.validate_documentation_index(root)

            self.assertTrue(any("SECURITY.md" in error and "missing" in error for error in required_errors))
            self.assertTrue(any("NOTICE" in error and "empty" in error for error in required_errors))
            self.assertTrue(any("SwiftDrop.ShareExtension.csproj" in error for error in required_errors))
            self.assertTrue(any("FINAL_REPOSITORY_STATUS.md" in error for error in index_errors))
            self.assertTrue(any("repository-completion-validation.md" in error for error in index_errors))
            self.assertTrue(any("manual-release-evidence-status.md" in error for error in index_errors))

    def test_release_evidence_status_assets_are_completion_requirements(self) -> None:
        self.assertIn("scripts/summarize_manual_release_evidence.py", completion.REQUIRED_PATHS)
        self.assertIn("docs/release/manual-release-evidence-status.md", completion.REQUIRED_PATHS)
        self.assertIn("scripts/summarize_manual_release_evidence.py", completion.RELEASE_CRITICAL_TRIGGER_PATHS)
        self.assertIn("release/manual-release-evidence-status.md", completion.DOC_INDEX_LINKS)

    def test_manual_release_template_must_remain_structurally_valid(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            template = root / completion.ALLOWED_PLACEHOLDER_FILE
            template.parent.mkdir(parents=True)
            template.write_text("{}\n", encoding="utf-8")

            errors = completion.validate_release_template(root)

            self.assertTrue(any("template is invalid" in error for error in errors))

    def test_placeholder_commit_is_allowed_only_in_canonical_template(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            template = root / completion.ALLOWED_PLACEHOLDER_FILE
            template.parent.mkdir(parents=True)
            template.write_text(f'{{"commit":"{completion.PLACEHOLDER_COMMIT}"}}\n', encoding="utf-8")

            self.assertEqual([], completion.validate_no_placeholder_leaks(root))

            leaked = root / "release-evidence" / "candidate.json"
            leaked.parent.mkdir(parents=True)
            leaked.write_text(f'{{"commit":"{completion.PLACEHOLDER_COMMIT}"}}\n', encoding="utf-8")
            errors = completion.validate_no_placeholder_leaks(root)

            self.assertTrue(any("placeholder leaked" in error and "candidate.json" in error for error in errors))


if __name__ == "__main__":
    unittest.main()
