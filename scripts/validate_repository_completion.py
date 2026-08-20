#!/usr/bin/env python3
"""Validate SwiftDrop's repository-completion invariants without third-party packages."""

from __future__ import annotations

import re
import sys
from pathlib import Path

from validate_manual_release_evidence import load_and_validate

REQUIRED_PATHS = (
    "README.md",
    "FINAL_REPOSITORY_STATUS.md",
    "BUILDING.md",
    "CHANGELOG.md",
    "PROJECT_STATUS.md",
    "NEXT_STEPS.md",
    "SECURITY.md",
    "SUPPORT.md",
    "CONTRIBUTING.md",
    "CODE_OF_CONDUCT.md",
    "PRIVACY.md",
    "TERMS.md",
    "LICENSE",
    "NOTICE",
    "THIRD_PARTY_NOTICES.md",
    "SwiftDrop.slnx",
    "Directory.Build.props",
    ".editorconfig",
    ".gitattributes",
    ".gitignore",
    ".github/CODEOWNERS",
    ".github/FUNDING.yml",
    ".github/dependabot.yml",
    ".github/PULL_REQUEST_TEMPLATE.md",
    ".github/ISSUE_TEMPLATE/bug_report.yml",
    ".github/ISSUE_TEMPLATE/feature_request.yml",
    ".github/ISSUE_TEMPLATE/config.yml",
    ".github/workflows/ci.yml",
    ".github/workflows/platform-builds.yml",
    ".github/workflows/codeql.yml",
    ".github/workflows/security-hygiene.yml",
    ".github/workflows/release-readiness.yml",
    "docs/README.md",
    "docs/repository-governance.md",
    "docs/testing/repository-completion-validation.md",
    "docs/release/release-checklist.md",
    "docs/release/release-process.md",
    "docs/release/dependency-evidence.md",
    "docs/release/signing-configuration.md",
    "docs/release/store-privacy-declarations.md",
    "docs/release/manual-release-evidence.md",
    "docs/release/manual-release-evidence-generator.md",
    "docs/release/manual-release-evidence.template.json",
    "docs/release/continuation-status-2026-08-19.md",
    "docs/release/repository-completion-2026-08-19.md",
    "what_changed.md",
    "what_changed_2026-08-19.md",
    "what_changed_2026-08-19_final.md",
    "what_changed_2026-08-19_closure.md",
    "scripts/verify-core.sh",
    "scripts/verify-core.ps1",
    "scripts/validate_documentation.py",
    "scripts/validate_localization.py",
    "scripts/validate_apple_integration.py",
    "scripts/validate_windows_integration.py",
    "scripts/validate_nuget_vulnerability_report.py",
    "scripts/create_dependency_evidence_manifest.py",
    "scripts/validate_manual_release_evidence.py",
    "scripts/create_manual_release_evidence.py",
    "scripts/validate_repository_completion.py",
)

REQUIRED_PROJECTS = (
    "src/SwiftDrop.Core/SwiftDrop.Core.csproj",
    "src/SwiftDrop.App/SwiftDrop.App.csproj",
    "src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj",
    "tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj",
    "benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj",
)

RELEASE_CRITICAL_TRIGGER_PATHS = (
    "scripts/verify-core.sh",
    "scripts/verify-core.ps1",
    "scripts/validate_documentation.py",
    "scripts/validate_localization.py",
    "scripts/validate_apple_integration.py",
    "scripts/validate_nuget_vulnerability_report.py",
    "scripts/create_dependency_evidence_manifest.py",
    "scripts/validate_windows_integration.py",
    "scripts/validate_manual_release_evidence.py",
    "scripts/create_manual_release_evidence.py",
    "scripts/validate_repository_completion.py",
    "scripts/tests/**",
)

DOC_INDEX_LINKS = (
    "../FINAL_REPOSITORY_STATUS.md",
    "repository-governance.md",
    "testing/repository-completion-validation.md",
    "release/repository-completion-2026-08-19.md",
    "../what_changed_2026-08-19_closure.md",
    "release/continuation-status-2026-08-19.md",
    "../what_changed_2026-08-19_final.md",
    "release/manual-release-evidence.md",
    "release/manual-release-evidence-generator.md",
)

CODEOWNERS_EXPECTATIONS = {
    "*": "@sanskarIN",
    "/.github/": "@sanskarIN",
    "/Directory.Build.props": "@sanskarIN",
    "/global.json": "@sanskarIN",
    "/scripts/": "@sanskarIN",
    "/src/SwiftDrop.Core/Security/": "@sanskarIN",
    "/src/SwiftDrop.Core/Protocol/": "@sanskarIN",
    "/src/SwiftDrop.Core/Networking/": "@sanskarIN",
    "/src/SwiftDrop.Core/Transfer/": "@sanskarIN",
    "/src/SwiftDrop.Core/Storage/": "@sanskarIN",
    "/src/SwiftDrop.App/Platforms/": "@sanskarIN",
    "/src/SwiftDrop.ShareExtension/": "@sanskarIN",
    "/SECURITY.md": "@sanskarIN",
    "/PRIVACY.md": "@sanskarIN",
    "/THIRD_PARTY_NOTICES.md": "@sanskarIN",
    "/docs/security/": "@sanskarIN",
    "/docs/protocol/": "@sanskarIN",
    "/docs/release/": "@sanskarIN",
}

SOURCE_SUFFIXES = {
    ".cs",
    ".xaml",
    ".csproj",
    ".props",
    ".targets",
    ".plist",
    ".xml",
    ".entitlements",
}
UNFINISHED_PATTERN = re.compile(
    r"\bTODO\b|\bFIXME\b|\bTBD\b|NotImplementedException|#warning"
)
PLACEHOLDER_COMMIT = "0" * 40
ALLOWED_PLACEHOLDER_FILE = Path("docs/release/manual-release-evidence.template.json")


def _read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def validate_required_paths(root: Path) -> list[str]:
    errors: list[str] = []
    for relative in REQUIRED_PATHS + REQUIRED_PROJECTS:
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


def validate_codeowners(root: Path) -> list[str]:
    path = root / ".github/CODEOWNERS"
    if not path.is_file():
        return ["repository ownership policy is missing: .github/CODEOWNERS"]
    try:
        text = _read_text(path)
    except (OSError, UnicodeError) as exc:
        return [f"could not read repository ownership policy: {exc}"]

    parsed: dict[str, tuple[str, ...]] = {}
    errors: list[str] = []
    for line_number, raw_line in enumerate(text.splitlines(), start=1):
        line = raw_line.strip()
        if not line or line.startswith("#"):
            continue
        fields = line.split()
        if len(fields) < 2:
            errors.append(f"CODEOWNERS entry has no owner at line {line_number}: {raw_line!r}")
            continue
        pattern, *owners = fields
        parsed[pattern] = tuple(owners)

    for pattern, owner in CODEOWNERS_EXPECTATIONS.items():
        owners = parsed.get(pattern)
        if owners is None:
            errors.append(f"CODEOWNERS is missing protected ownership entry: {pattern}")
        elif owner not in owners:
            errors.append(
                f"CODEOWNERS entry {pattern!r} must retain protected owner {owner}; "
                f"found {' '.join(owners)}"
            )
    return errors


def validate_release_template(root: Path) -> list[str]:
    template = root / ALLOWED_PLACEHOLDER_FILE
    if not template.is_file():
        return ["manual release evidence template is missing"]
    try:
        load_and_validate(template)
    except (OSError, ValueError) as exc:
        return [f"manual release evidence template is invalid: {exc}"]
    return []


def validate_no_placeholder_leaks(root: Path) -> list[str]:
    errors: list[str] = []
    for path in sorted(root.rglob("*.json")):
        if not path.is_file():
            continue
        relative = path.relative_to(root)
        if relative == ALLOWED_PLACEHOLDER_FILE:
            continue
        try:
            text = _read_text(path)
        except (OSError, UnicodeError):
            continue
        if PLACEHOLDER_COMMIT in text:
            errors.append(
                "all-zero release-candidate placeholder leaked outside canonical template: "
                f"{relative.as_posix()}"
            )
    return errors


def validate_repository(root: Path) -> list[str]:
    errors: list[str] = []
    errors.extend(validate_required_paths(root))
    errors.extend(validate_no_unfinished_markers(root))
    errors.extend(validate_release_readiness_triggers(root))
    errors.extend(validate_portable_verifier_integration(root))
    errors.extend(validate_documentation_index(root))
    errors.extend(validate_codeowners(root))
    errors.extend(validate_release_template(root))
    errors.extend(validate_no_placeholder_leaks(root))
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
