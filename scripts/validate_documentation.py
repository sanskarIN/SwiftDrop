#!/usr/bin/env python3
"""Validate SwiftDrop's canonical documentation surface and local Markdown links."""

from __future__ import annotations

import re
import sys
from pathlib import Path
from urllib.parse import unquote, urlsplit

ROOT = Path(__file__).resolve().parents[1]

REQUIRED_DOCUMENTS = (
    "README.md",
    "FINAL_REPOSITORY_STATUS.md",
    "BUILDING.md",
    "CHANGELOG.md",
    "CODE_OF_CONDUCT.md",
    "CONTRIBUTING.md",
    "DECISIONS.md",
    "NEXT_STEPS.md",
    "PRIVACY.md",
    "PROJECT_STATUS.md",
    "SECURITY.md",
    "SUPPORT.md",
    "TERMS.md",
    "THIRD_PARTY_NOTICES.md",
    "what_changed.md",
    "what_changed_2026-08-19.md",
    "what_changed_2026-08-19_final.md",
    "what_changed_2026-08-19_closure.md",
    "docs/README.md",
    "docs/installation.md",
    "docs/user-guide.md",
    "docs/configuration.md",
    "docs/faq.md",
    "docs/glossary.md",
    "docs/troubleshooting.md",
    "docs/networking.md",
    "docs/development-guide.md",
    "docs/diagnostics-and-bug-reports.md",
    "docs/architecture.md",
    "docs/architecture/clean-architecture.md",
    "docs/architecture/project-structure.md",
    "docs/platform-permissions.md",
    "docs/platform/integration-status.md",
    "docs/protocol/wire-format.md",
    "docs/protocol/security.md",
    "docs/protocol/compatibility.md",
    "docs/protocol/compatibility-matrix.md",
    "docs/security/THREAT_MODEL.md",
    "docs/storage/database-schema.md",
    "docs/testing/ci-reference.md",
    "docs/testing/repository-completion-validation.md",
    "docs/testing/deterministic-state-models.md",
    "docs/testing/security-test-plan.md",
    "docs/testing/manual-test-matrix.md",
    "docs/testing/release-candidate-additional-cases.md",
    "docs/testing/accessibility-checklist.md",
    "docs/testing/performance-benchmarks.md",
    "docs/release/continuation-status-2026-08-19.md",
    "docs/release/repository-completion-2026-08-19.md",
    "docs/release/final-audit-2026-08-18.md",
    "docs/release/release-process.md",
    "docs/release/release-checklist.md",
    "docs/release/dependency-evidence.md",
    "docs/release/signing-configuration.md",
    "docs/release/store-privacy-declarations.md",
    "docs/release/manual-release-evidence.md",
    "docs/release/manual-release-evidence-generator.md",
    "docs/versioning-and-compatibility.md",
)

INDEX_LINKS = (
    "installation.md",
    "user-guide.md",
    "configuration.md",
    "faq.md",
    "glossary.md",
    "troubleshooting.md",
    "networking.md",
    "development-guide.md",
    "diagnostics-and-bug-reports.md",
    "architecture/project-structure.md",
    "testing/ci-reference.md",
    "testing/repository-completion-validation.md",
    "testing/deterministic-state-models.md",
    "release/repository-completion-2026-08-19.md",
    "release/continuation-status-2026-08-19.md",
    "release/release-process.md",
    "release/manual-release-evidence.md",
    "release/manual-release-evidence-generator.md",
    "release/dependency-evidence.md",
    "versioning-and-compatibility.md",
)

INLINE_LINK_RE = re.compile(r"!?\[[^\]]*\]\(([^)]+)\)")
SKIP_DIRS = {".git", "bin", "obj", "node_modules"}


def markdown_files() -> list[Path]:
    files: list[Path] = []
    for path in ROOT.rglob("*.md"):
        if any(part in SKIP_DIRS for part in path.relative_to(ROOT).parts):
            continue
        files.append(path)
    return sorted(files)


def destination_token(raw: str) -> str:
    value = raw.strip()
    if not value:
        return ""
    if value.startswith("<"):
        close = value.find(">")
        if close > 0:
            return value[1:close]
    return value.split(maxsplit=1)[0]


def is_external_or_anchor(destination: str) -> bool:
    if not destination or destination.startswith("#"):
        return True
    split = urlsplit(destination)
    if split.scheme or split.netloc:
        return True
    return destination.startswith("/")


def validate_required(errors: list[str]) -> None:
    for relative in REQUIRED_DOCUMENTS:
        path = ROOT / relative
        if not path.is_file():
            errors.append(f"Missing required documentation file: {relative}")
        elif path.stat().st_size == 0:
            errors.append(f"Required documentation file is empty: {relative}")


def validate_index(errors: list[str]) -> None:
    index = ROOT / "docs/README.md"
    if not index.is_file():
        return
    text = index.read_text(encoding="utf-8")
    for link in INDEX_LINKS:
        if f"({link})" not in text:
            errors.append(f"docs/README.md does not link canonical guide: {link}")

    root_readme = (ROOT / "README.md").read_text(encoding="utf-8")
    if "docs/README.md" not in root_readme:
        errors.append("README.md does not link the canonical docs/README.md index")


def validate_local_links(errors: list[str]) -> int:
    checked = 0
    root_resolved = ROOT.resolve()
    for markdown in markdown_files():
        text = markdown.read_text(encoding="utf-8")
        for match in INLINE_LINK_RE.finditer(text):
            destination = destination_token(match.group(1))
            if is_external_or_anchor(destination):
                continue
            split = urlsplit(destination)
            relative_path = unquote(split.path)
            if not relative_path:
                continue
            checked += 1
            target = (markdown.parent / relative_path).resolve()
            try:
                target.relative_to(root_resolved)
            except ValueError:
                errors.append(f"{markdown.relative_to(ROOT)} links outside repository: {destination}")
                continue
            if not target.exists():
                errors.append(
                    f"Broken local Markdown link in {markdown.relative_to(ROOT)}: "
                    f"{destination} -> {target.relative_to(ROOT)}"
                )
    return checked


def validate_no_completed_helpers(errors: list[str]) -> None:
    forbidden = (
        ROOT / ".github/workflows/one-time-documentation-completion.yml",
        ROOT / "scripts/documentation_completion_sync.py",
        ROOT / ".github/workflows/one-time-final-documentation-sync.yml",
        ROOT / "scripts/final_documentation_state_sync.py",
        ROOT / ".github/workflows/one-time-repository-documentation-finalize.yml",
        ROOT / "scripts/repository_documentation_finalize.py",
        ROOT / ".github/workflows/one-time-continuation-release-evidence-sync.yml",
        ROOT / "scripts/continuation_release_evidence_sync.py",
        ROOT / ".github/workflows/one-time-final-windows-sqlite-sync.yml",
        ROOT / "scripts/final_windows_sqlite_sync.py",
    )
    for path in forbidden:
        if path.exists():
            errors.append(
                f"Completed one-time documentation helper must not remain: {path.relative_to(ROOT)}"
            )


def main() -> int:
    errors: list[str] = []
    validate_required(errors)
    validate_index(errors)
    links_checked = validate_local_links(errors)
    validate_no_completed_helpers(errors)
    if errors:
        print("Documentation validation failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1
    print(
        f"Documentation validation passed: {len(REQUIRED_DOCUMENTS)} required files "
        f"and {links_checked} local Markdown links checked."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
