#!/usr/bin/env python3
"""Fail when SwiftDrop's repository-complete source contract regresses."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

REQUIRED_FILES = (
    "README.md",
    "LICENSE",
    "NOTICE",
    "SECURITY.md",
    "SUPPORT.md",
    "CONTRIBUTING.md",
    "CODE_OF_CONDUCT.md",
    "PRIVACY.md",
    "TERMS.md",
    "CHANGELOG.md",
    "BUILDING.md",
    "THIRD_PARTY_NOTICES.md",
    "PROJECT_STATUS.md",
    "NEXT_STEPS.md",
    "SwiftDrop.slnx",
    "Directory.Build.props",
    ".editorconfig",
    ".gitattributes",
    ".gitignore",
    ".github/workflows/ci.yml",
    ".github/workflows/codeql.yml",
    ".github/workflows/security-hygiene.yml",
    ".github/workflows/release-readiness.yml",
    "docs/README.md",
    "docs/release/release-process.md",
    "docs/release/release-checklist.md",
    "docs/release/dependency-evidence.md",
    "docs/release/signing-configuration.md",
    "docs/release/store-privacy-declarations.md",
    "docs/release/manual-release-evidence.md",
    "docs/release/manual-release-evidence.template.json",
    "docs/release/manual-release-evidence-generator.md",
    "docs/release/continuation-status-2026-08-19.md",
    "scripts/verify-core.sh",
    "scripts/verify-core.ps1",
    "scripts/validate_documentation.py",
    "scripts/validate_localization.py",
    "scripts/validate_apple_integration.py",
    "scripts/validate_windows_integration.py",
    "scripts/validate_nuget_vulnerability_report.py",
    "scripts/validate_manual_release_evidence.py",
    "scripts/create_manual_release_evidence.py",
)

REQUIRED_PROJECTS = (
    "src/SwiftDrop.Core/SwiftDrop.Core.csproj",
    "src/SwiftDrop.App/SwiftDrop.App.csproj",
    "src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj",
    "tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj",
    "benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj",
)

SOURCE_SUFFIXES = {".cs", ".xaml", ".csproj", ".props", ".targets", ".plist", ".xml"}
FORBIDDEN_SOURCE_MARKERS = (
    "TODO",
    "FIXME",
    "NotImplementedException",
    "#warning",
)

PLACEHOLDER_COMMIT = "0" * 40
ALLOWED_PLACEHOLDER_FILE = Path("docs/release/manual-release-evidence.template.json")


def _relative(path: Path, root: Path) -> str:
    return path.relative_to(root).as_posix()


def validate_repository(root: Path) -> list[str]:
    root = root.resolve()
    errors: list[str] = []

    if not root.is_dir():
        return [f"repository root is not a directory: {root}"]

    for relative in REQUIRED_FILES + REQUIRED_PROJECTS:
        path = root / relative
        if not path.is_file():
            errors.append(f"missing required file: {relative}")
        elif path.stat().st_size == 0:
            errors.append(f"required file is empty: {relative}")

    source_root = root / "src"
    if not source_root.is_dir():
        errors.append("missing production source directory: src")
    else:
        for path in sorted(source_root.rglob("*")):
            if not path.is_file() or path.suffix.lower() not in SOURCE_SUFFIXES:
                continue
            try:
                text = path.read_text(encoding="utf-8")
            except UnicodeDecodeError:
                errors.append(f"production source is not UTF-8 text: {_relative(path, root)}")
                continue
            for marker in FORBIDDEN_SOURCE_MARKERS:
                if marker in text:
                    errors.append(
                        f"unfinished implementation marker {marker!r} in {_relative(path, root)}"
                    )

    for path in sorted(root.rglob("*.json")):
        if not path.is_file():
            continue
        relative = path.relative_to(root)
        if relative == ALLOWED_PLACEHOLDER_FILE:
            continue
        try:
            text = path.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            continue
        if PLACEHOLDER_COMMIT in text:
            errors.append(
                f"all-zero release-candidate placeholder leaked outside canonical template: {relative.as_posix()}"
            )

    return errors


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--root",
        type=Path,
        default=Path(__file__).resolve().parents[1],
        help="repository root; defaults to the parent of scripts/",
    )
    return parser.parse_args(argv)


def main(argv: list[str]) -> int:
    args = parse_args(argv[1:])
    errors = validate_repository(args.root)
    if errors:
        print("repository completion validation failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print(
        "Repository completion validation passed: required project/community/release surface present, "
        "no unfinished production markers, no leaked placeholder candidate."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
