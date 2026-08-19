#!/usr/bin/env python3
"""Validate SwiftDrop manual release-candidate evidence without third-party packages."""

from __future__ import annotations

import json
import re
import sys
from datetime import datetime
from pathlib import Path
from typing import Any

SCHEMA_VERSION = 1
VALID_STATUSES = {"not-run", "in-progress", "blocked", "passed", "failed"}
COMMIT_RE = re.compile(r"^[0-9a-f]{40}$")
PLACEHOLDER_COMMIT = "0" * 40

REQUIRED_CASES: dict[str, tuple[str, ...]] = {
    "android": (
        "signed-install-upgrade",
        "share-provider-metadata",
        "foreground-background",
        "multicast-discovery",
    ),
    "windows": (
        "signed-msix-install-upgrade",
        "protocol-activation",
        "firewall-network",
        "folder-picker-drop",
    ),
    "ios": (
        "signed-device-build",
        "share-extension",
        "app-group-handoff",
        "item-provider",
    ),
    "maccatalyst": (
        "signed-notarized-build",
        "sandbox-network",
        "native-drop",
    ),
    "cross-device": (
        "pairing-methods",
        "file-folder-text",
        "pause-cancel-resume",
        "network-switching",
        "low-storage",
    ),
    "filesystem": (
        "symlink-reparse",
        "collision-pressure",
        "destination-mutation",
    ),
    "accessibility-localization": (
        "screen-readers",
        "large-text-high-contrast",
        "hindi-layout",
    ),
    "dependency-license": (
        "exact-graph-audit",
        "licenses-notices",
        "provenance",
    ),
    "store": (
        "privacy-declarations",
        "metadata-screenshots",
        "submission-signing",
    ),
}

TOP_LEVEL_KEYS = {"schema_version", "candidate", "groups"}
CANDIDATE_KEYS = {"commit", "version", "created_utc"}
GROUP_KEYS = {"id", "status", "cases"}
CASE_KEYS = {"id", "status", "executed_utc", "environment", "evidence", "notes"}
FORBIDDEN_TEXT_MARKERS = (
    "-----begin private key-----",
    "-----begin ec private key-----",
    "swiftdrop://pair",
)


def _error(path: str, message: str) -> ValueError:
    return ValueError(f"{path}: {message}")


def _require_exact_keys(value: dict[str, Any], expected: set[str], path: str) -> None:
    actual = set(value)
    missing = sorted(expected - actual)
    unknown = sorted(actual - expected)
    if missing:
        raise _error(path, f"missing field(s): {', '.join(missing)}")
    if unknown:
        raise _error(path, f"unknown field(s): {', '.join(unknown)}")


def _require_text(value: Any, path: str, *, maximum: int, allow_empty: bool = False) -> str:
    if not isinstance(value, str):
        raise _error(path, "must be a string")
    if not allow_empty and not value:
        raise _error(path, "must not be empty")
    if len(value) > maximum:
        raise _error(path, f"must be at most {maximum} characters")
    if any(ord(ch) < 32 and ch not in "\t\n\r" for ch in value):
        raise _error(path, "contains a control character")
    return value


def _reject_sensitive_text(value: str, path: str) -> None:
    lowered = value.lower()
    if any(marker in lowered for marker in FORBIDDEN_TEXT_MARKERS):
        raise _error(path, "must not contain private-key material or a pairing capability")


def _parse_utc(value: Any, path: str, *, nullable: bool) -> datetime | None:
    if value is None:
        if nullable:
            return None
        raise _error(path, "must be an RFC 3339 UTC timestamp")
    text = _require_text(value, path, maximum=40)
    if not text.endswith("Z"):
        raise _error(path, "must use the canonical UTC 'Z' suffix")
    try:
        parsed = datetime.fromisoformat(text[:-1] + "+00:00")
    except ValueError as exc:
        raise _error(path, "must be a valid RFC 3339 UTC timestamp") from exc
    if parsed.utcoffset() is None or parsed.utcoffset().total_seconds() != 0:
        raise _error(path, "must be UTC")
    return parsed


def _validate_evidence(values: Any, path: str) -> None:
    if not isinstance(values, list):
        raise _error(path, "must be an array")
    if len(values) > 32:
        raise _error(path, "must contain at most 32 references")
    seen: set[str] = set()
    for index, value in enumerate(values):
        item_path = f"{path}[{index}]"
        reference = _require_text(value, item_path, maximum=512)
        if reference != reference.strip():
            raise _error(item_path, "must not have surrounding whitespace")
        if reference in seen:
            raise _error(item_path, "duplicates an earlier evidence reference")
        if "\n" in reference or "\r" in reference:
            raise _error(item_path, "must be a single-line reference")
        _reject_sensitive_text(reference, item_path)
        seen.add(reference)


def _validate_notes(value: Any, path: str) -> None:
    notes = _require_text(value, path, maximum=2000, allow_empty=True)
    _reject_sensitive_text(notes, path)


def _aggregate_status(statuses: list[str]) -> str:
    if any(status == "failed" for status in statuses):
        return "failed"
    if statuses and all(status == "passed" for status in statuses):
        return "passed"
    if statuses and all(status == "not-run" for status in statuses):
        return "not-run"
    if any(status == "blocked" for status in statuses) and all(
        status in {"passed", "blocked", "not-run"} for status in statuses
    ):
        return "blocked"
    return "in-progress"


def _validate_case(case: Any, group_id: str, index: int) -> tuple[str, str]:
    path = f"groups[{group_id}].cases[{index}]"
    if not isinstance(case, dict):
        raise _error(path, "must be an object")
    _require_exact_keys(case, CASE_KEYS, path)

    case_id = _require_text(case["id"], f"{path}.id", maximum=80)
    status = _require_text(case["status"], f"{path}.status", maximum=20)
    if status not in VALID_STATUSES:
        raise _error(f"{path}.status", f"must be one of {sorted(VALID_STATUSES)}")

    executed = _parse_utc(case["executed_utc"], f"{path}.executed_utc", nullable=True)
    environment = _require_text(case["environment"], f"{path}.environment", maximum=300, allow_empty=True)
    _reject_sensitive_text(environment, f"{path}.environment")
    _validate_evidence(case["evidence"], f"{path}.evidence")
    _validate_notes(case["notes"], f"{path}.notes")

    if status in {"passed", "failed"}:
        if executed is None:
            raise _error(f"{path}.executed_utc", f"is required when status is {status}")
        if not environment.strip():
            raise _error(f"{path}.environment", f"is required when status is {status}")
        if not case["evidence"]:
            raise _error(f"{path}.evidence", f"requires at least one reference when status is {status}")
    elif status == "blocked" and not case["notes"].strip():
        raise _error(f"{path}.notes", "is required when status is blocked")
    elif status == "not-run":
        if executed is not None:
            raise _error(f"{path}.executed_utc", "must be null when status is not-run")
        if case["evidence"]:
            raise _error(f"{path}.evidence", "must be empty when status is not-run")

    return case_id, status


def validate_document(document: Any, *, require_complete: bool = False) -> None:
    if not isinstance(document, dict):
        raise _error("root", "must be an object")
    _require_exact_keys(document, TOP_LEVEL_KEYS, "root")

    if document["schema_version"] != SCHEMA_VERSION:
        raise _error("schema_version", f"must equal {SCHEMA_VERSION}")

    candidate = document["candidate"]
    if not isinstance(candidate, dict):
        raise _error("candidate", "must be an object")
    _require_exact_keys(candidate, CANDIDATE_KEYS, "candidate")

    commit = _require_text(candidate["commit"], "candidate.commit", maximum=40)
    if not COMMIT_RE.fullmatch(commit):
        raise _error("candidate.commit", "must be a canonical lowercase 40-hex commit SHA")
    if require_complete and commit == PLACEHOLDER_COMMIT:
        raise _error("candidate.commit", "must not use the all-zero template placeholder in complete mode")
    version = _require_text(candidate["version"], "candidate.version", maximum=64)
    if version != version.strip():
        raise _error("candidate.version", "must not have surrounding whitespace")
    _parse_utc(candidate["created_utc"], "candidate.created_utc", nullable=False)

    groups = document["groups"]
    if not isinstance(groups, list):
        raise _error("groups", "must be an array")
    if len(groups) != len(REQUIRED_CASES):
        raise _error("groups", f"must contain exactly {len(REQUIRED_CASES)} required groups")

    seen_groups: set[str] = set()
    for index, group in enumerate(groups):
        path = f"groups[{index}]"
        if not isinstance(group, dict):
            raise _error(path, "must be an object")
        _require_exact_keys(group, GROUP_KEYS, path)
        group_id = _require_text(group["id"], f"{path}.id", maximum=80)
        if group_id not in REQUIRED_CASES:
            raise _error(f"{path}.id", "is not a recognized required release group")
        if group_id in seen_groups:
            raise _error(f"{path}.id", "duplicates an earlier group")
        seen_groups.add(group_id)

        status = _require_text(group["status"], f"{path}.status", maximum=20)
        if status not in VALID_STATUSES:
            raise _error(f"{path}.status", f"must be one of {sorted(VALID_STATUSES)}")

        cases = group["cases"]
        if not isinstance(cases, list):
            raise _error(f"{path}.cases", "must be an array")
        expected_case_ids = set(REQUIRED_CASES[group_id])
        if len(cases) != len(expected_case_ids):
            raise _error(f"{path}.cases", f"must contain exactly {len(expected_case_ids)} required cases")

        seen_cases: set[str] = set()
        case_statuses: list[str] = []
        for case_index, case in enumerate(cases):
            case_id, case_status = _validate_case(case, group_id, case_index)
            if case_id not in expected_case_ids:
                raise _error(f"{path}.cases[{case_index}].id", "is not required for this group")
            if case_id in seen_cases:
                raise _error(f"{path}.cases[{case_index}].id", "duplicates an earlier case")
            seen_cases.add(case_id)
            case_statuses.append(case_status)

        missing_cases = sorted(expected_case_ids - seen_cases)
        if missing_cases:
            raise _error(f"{path}.cases", f"missing required case(s): {', '.join(missing_cases)}")

        expected_status = _aggregate_status(case_statuses)
        if status != expected_status:
            raise _error(
                f"{path}.status",
                f"must be {expected_status!r} for the recorded case states, not {status!r}",
            )
        if require_complete and status != "passed":
            raise _error(f"{path}.status", "must be 'passed' in complete release-candidate mode")

    missing_groups = sorted(set(REQUIRED_CASES) - seen_groups)
    if missing_groups:
        raise _error("groups", f"missing required group(s): {', '.join(missing_groups)}")


def load_and_validate(path: Path, *, require_complete: bool = False) -> None:
    try:
        document = json.loads(path.read_text(encoding="utf-8"))
    except OSError as exc:
        raise ValueError(f"could not read {path}: {exc}") from exc
    except json.JSONDecodeError as exc:
        raise ValueError(f"invalid JSON in {path}: {exc}") from exc
    validate_document(document, require_complete=require_complete)


def main(argv: list[str]) -> int:
    args = argv[1:]
    require_complete = False
    if args and args[0] == "--require-complete":
        require_complete = True
        args = args[1:]
    if len(args) != 1:
        print(
            f"usage: {Path(argv[0]).name} [--require-complete] <manual-release-evidence.json>",
            file=sys.stderr,
        )
        return 2
    try:
        load_and_validate(Path(args[0]), require_complete=require_complete)
    except ValueError as exc:
        print(f"manual release evidence invalid: {exc}", file=sys.stderr)
        return 1
    mode = "complete" if require_complete else "structural"
    print(f"manual release evidence valid ({mode} mode)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
