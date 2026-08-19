#!/usr/bin/env python3
"""Create a new SwiftDrop manual release-evidence record in an honest not-run state."""

from __future__ import annotations

import argparse
import json
import sys
from datetime import datetime, timezone
from pathlib import Path

from validate_manual_release_evidence import PLACEHOLDER_COMMIT, REQUIRED_CASES, validate_document


def canonical_now_utc() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds").replace("+00:00", "Z")


def build_document(commit: str, version: str, created_utc: str) -> dict[str, object]:
    if commit == PLACEHOLDER_COMMIT:
        raise ValueError("candidate commit must not use the all-zero template placeholder")

    document: dict[str, object] = {
        "schema_version": 1,
        "candidate": {
            "commit": commit,
            "version": version,
            "created_utc": created_utc,
        },
        "groups": [
            {
                "id": group_id,
                "status": "not-run",
                "cases": [
                    {
                        "id": case_id,
                        "status": "not-run",
                        "executed_utc": None,
                        "environment": "",
                        "evidence": [],
                        "notes": "",
                    }
                    for case_id in case_ids
                ],
            }
            for group_id, case_ids in REQUIRED_CASES.items()
        ],
    }
    validate_document(document)
    return document


def write_document(document: dict[str, object], output: Path, *, force: bool) -> None:
    if output.is_symlink():
        raise ValueError(f"output must not be a symbolic link: {output}")
    if output.exists() and not force:
        raise ValueError(f"output already exists: {output} (use --force to replace it)")
    if output.exists() and not output.is_file():
        raise ValueError(f"output exists but is not a regular file: {output}")

    output.parent.mkdir(parents=True, exist_ok=True)
    rendered = json.dumps(document, indent=2, ensure_ascii=False) + "\n"
    if force:
        output.write_text(rendered, encoding="utf-8")
        return

    try:
        with output.open("x", encoding="utf-8", newline="\n") as stream:
            stream.write(rendered)
    except FileExistsError as exc:
        raise ValueError(f"output already exists: {output} (use --force to replace it)") from exc


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--commit", required=True, help="exact lowercase 40-hex release-candidate commit SHA")
    parser.add_argument("--version", required=True, help="release-candidate version label")
    parser.add_argument("--output", required=True, type=Path, help="destination JSON evidence manifest")
    parser.add_argument(
        "--created-utc",
        default=None,
        help="optional canonical UTC timestamp; defaults to current UTC",
    )
    parser.add_argument("--force", action="store_true", help="replace an existing regular output file")
    return parser.parse_args(argv)


def main(argv: list[str]) -> int:
    args = parse_args(argv[1:])
    created_utc = args.created_utc or canonical_now_utc()
    try:
        document = build_document(args.commit, args.version, created_utc)
        write_document(document, args.output, force=args.force)
    except (OSError, ValueError) as exc:
        print(f"could not create manual release evidence: {exc}", file=sys.stderr)
        return 1

    print(f"created manual release evidence: {args.output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
