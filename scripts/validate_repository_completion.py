#!/usr/bin/env python3
"""Validate SwiftDrop's repository-completion invariants without third-party packages."""

from __future__ import annotations

import re
import sys
from pathlib import Path

from validate_manual_release_evidence import load_and_validate

REQUIRED_PATHS = (
    "README.md",
    "BUILDING.md",
    "CHANGELOG.md",
    "PROJECT_STATUS.md",
    "NEXT_STEPS.md",
    "SECURITY.md",
    "PRIVACY.md",
    "THIRD_PARTY_NOTICES.md",
    "docs/README.md",
    "docs/release/release-checklist.md",
    "docs/release/release-process.md",
    "docs/release/manual-release-evidence.md",
    "docs/release/manual-release-evidence-generator.md",
    "docs/release/manual-release-evidence.template.json",
    "docs/release/continuation-status-2026-08-19.md",
    "docs/release/repository-completion-2026-08-19.md",
    "what_changed.md",
    "what_changed_2026-08-19.md",
    "what_changed_2026-08-19_final.md",
    "scripts/validate_manual_release_evidence.py",
    "scripts/create_manual_release_evidence.py",
    "scripts/validate_repository_completion.py",
    ".github/workflows/ci.yml",
    ".github/workflows/release-readiness.yml",
)

RELEASE_CRITICAL_TRIGGER_PATHS = (
    "scripts/verify-core.sh",
    "scripts/verify-core.ps1",
    "scripts/validate_nuget_vulnerability_report.py",
    "scripts/create_dependency_evidence_manifest.py",
    "scripts/validate_windows_integration.py",
    "scripts/validate_manual_release_evidence.py",
    "scripts/create_manual_release_evidence.py",
    "scripts/validate_repository_completion.py",
    "scripts/tests/**",
)

DOC_INDEX_LINKS = (
    "release/repository-completion-2026-08-19.md",
    "release/continuation-status-2026-08-19.md",
    "../what_changed_2026-08-19_final.md",
    "release/manual-release-evidence.md",
    "release/manual-release-evidence-generator.md",
)

SOURCE_SUFFIXES = {".cs", ".xaml", ".csproj", ".props", ".targets"}
UNFINISHED_PATTERN = re.compile(r"\bTODO\b|\bFIXME\b|\bTBD\b|NotImplementedException")


def _read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def validate_required_paths(root: Path) -> list[str]:
    errors: list[str] = []
    for relative in REQUIRED_PATHS:
        path = root / relative
        if not path.is_file():
            errors.append(f"required repository file is missing: {relative}")
            continue
        try:
            if not _read_text(path).strip():
                errors.append(f"required repository file is empty: {relative}")
        except (OSError, UnicodeError) as exc:
            errors.append(f"could not read required repository file {relative}: {exc}")
    return errors


def validate_no_unfinished_markers(root: Path) -> list[str]:
    errors: list[str] = []
    source_root = root / "src"
    if not source_root.is_dir():
        return ["production source directory is missing: src"]

    for path in sorted(source_root.rglob("*")):
        if not path.is_file() or path.suffix.lower() not in SOURCE_SUFFIXES:
            continue
        try:
            text = _read_text(path)
        except (OSError, UnicodeError) as exc:
            errors.append(f"could not inspect production source {path.relative_to(root)}: {exc}")
            continue
        match = UNFINISHED_PATTERN.search(text)
        if match:
            line = text.count("\n", 0, match.start()) + 1
            errors.append(
                f"unfinished marker {match.group(0)!r} found in production source "
                f"{path.relative_to(root)}:{line}"
            )
    return errors


def validate_release_readiness_triggers(root: Path) -> list[str]:
    workflow = root / ".github/workflows/release-readiness.yml"
    if not workflow.is_file():
        return ["release-readiness workflow is missing"]
    try:
        text = _read_text(workflow)
    except (OSError, UnicodeError) as exc:
        return [f"could not read release-readiness workflow: {exc}"]

    errors: list[str] = []
    for relative in RELEASE_CRITICAL_TRIGGER_PATHS:
        needle = f"- '{relative}'"
        count = text.count(needle)
        if count != 2:
            errors.append(
                f"release-readiness trigger must contain {relative!r} exactly twice "
                f"(push and pull_request); found {count}"
            )
    return errors


def validate_portable_verifier_integration(root: Path) -> list[str]:
    expectations = {
        ".github/workflows/ci.yml": "python3 scripts/validate_repository_completion.py",
        "scripts/verify-core.sh": "python3 scripts/validate_repository_completion.py",
        "scripts/verify-core.ps1": "scripts/validate_repository_completion.py",
    }
    errors: list[str] = []
    for relative, needle in expectations.items():
        path = root / relative
        if not path.is_file():
            errors.append(f"completion-validator integration target is missing: {relative}")
            continue
        try:
            text = _read_text(path)
        except (OSError, UnicodeError) as exc:
            errors.append(f"could not read completion-validator integration target {relative}: {exc}")
            continue
        if needle not in text:
            errors.append(f"{relative} does not execute the repository completion validator")
    return errors


def validate_documentation_index(root: Path) -> list[str]:
    index = root / "docs/README.md"
    if not index.is_file():
        return ["canonical documentation index is missing: docs/README.md"]
    try:
        text = _read_text(index)
    except (OSError, UnicodeError) as exc:
        return [f"could not read canonical documentation index: {exc}"]

    errors: list[str] = []
    for link in DOC_INDEX_LINKS:
        if link not in text:
            errors.append(f"canonical documentation index is missing final link: {link}")
    return errors


def validate_release_template(root: Path) -> list[str]:
    template = root / "docs/release/manual-release-evidence.template.json"
    if not template.is_file():
        return ["manual release evidence template is missing"]
    try:
        load_and_validate(template)
    except ValueError as exc:
        return [f"manual release evidence template is invalid: {exc}"]
    return []


def validate_repository(root: Path) -> list[str]:
    errors: list[str] = []
    errors.extend(validate_required_paths(root))
    errors.extend(validate_no_unfinished_markers(root))
    errors.extend(validate_release_readiness_triggers(root))
    errors.extend(validate_portable_verifier_integration(root))
    errors.extend(validate_documentation_index(root))
    errors.extend(validate_release_template(root))
    return errors


def main(argv: list[str]) -> int:
    if len(argv) > 2:
        print(f"usage: {Path(argv[0]).name} [repository-root]", file=sys.stderr)
        return 2

    root = Path(argv[1]).resolve() if len(argv) == 2 else Path(__file__).resolve().parents[1]
    errors = validate_repository(root)
    if errors:
        for error in errors:
            print(f"repository completion invalid: {error}", file=sys.stderr)
        return 1

    print("repository completion contract valid")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
