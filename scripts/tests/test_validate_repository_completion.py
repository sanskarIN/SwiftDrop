import importlib.util
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).resolve().parents[1] / "validate_repository_completion.py"
SPEC = importlib.util.spec_from_file_location("validate_repository_completion", SCRIPT)
assert SPEC is not None and SPEC.loader is not None
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class RepositoryCompletionValidatorTests(unittest.TestCase):
    def create_complete_repository(self, root: Path) -> None:
        for relative in MODULE.REQUIRED_FILES + MODULE.REQUIRED_PROJECTS:
            path = root / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("complete\n", encoding="utf-8")

        template = root / MODULE.ALLOWED_PLACEHOLDER_FILE
        template.write_text(f'{{"commit":"{MODULE.PLACEHOLDER_COMMIT}"}}\n', encoding="utf-8")

    def test_complete_repository_passes(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.create_complete_repository(root)

            self.assertEqual([], MODULE.validate_repository(root))

    def test_missing_required_file_fails(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.create_complete_repository(root)
            (root / "SECURITY.md").unlink()

            errors = MODULE.validate_repository(root)
            self.assertIn("missing required file: SECURITY.md", errors)

    def test_missing_required_project_fails(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.create_complete_repository(root)
            project = root / "src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj"
            project.unlink()

            errors = MODULE.validate_repository(root)
            self.assertIn(
                "missing required file: src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj",
                errors,
            )

    def test_empty_required_file_fails(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.create_complete_repository(root)
            (root / "NOTICE").write_text("", encoding="utf-8")

            errors = MODULE.validate_repository(root)
            self.assertIn("required file is empty: NOTICE", errors)

    def test_unfinished_source_marker_fails(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.create_complete_repository(root)
            source = root / "src/SwiftDrop.Core/Incomplete.cs"
            source.write_text("// TODO finish later\n", encoding="utf-8")

            errors = MODULE.validate_repository(root)
            self.assertTrue(any("unfinished implementation marker 'TODO'" in error for error in errors))

    def test_non_utf8_production_source_fails(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.create_complete_repository(root)
            source = root / "src/SwiftDrop.Core/Invalid.cs"
            source.write_bytes(b"\xff\xfe\x00\x00")

            errors = MODULE.validate_repository(root)
            self.assertIn("production source is not UTF-8 text: src/SwiftDrop.Core/Invalid.cs", errors)

    def test_template_is_allowed_to_use_placeholder_commit(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.create_complete_repository(root)

            errors = MODULE.validate_repository(root)
            self.assertFalse(any("placeholder leaked" in error for error in errors))

    def test_placeholder_commit_outside_template_fails(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.create_complete_repository(root)
            leaked = root / "release-evidence/candidate.json"
            leaked.parent.mkdir(parents=True, exist_ok=True)
            leaked.write_text(f'{{"commit":"{MODULE.PLACEHOLDER_COMMIT}"}}\n', encoding="utf-8")

            errors = MODULE.validate_repository(root)
            self.assertIn(
                "all-zero release-candidate placeholder leaked outside canonical template: "
                "release-evidence/candidate.json",
                errors,
            )


if __name__ == "__main__":
    unittest.main()
