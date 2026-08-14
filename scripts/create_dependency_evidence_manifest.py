#!/usr/bin/env python3
"""Create a deterministic SHA-256 manifest for dependency-evidence JSON files."""

from __future__ import annotations

import argparse
import hashlib
import json
import sys
from pathlib import Path


def _resolve_within(root: Path, candidate: Path) -> Path:
    root_resolved = root.resolve()
    candidate_resolved = candidate.resolve()
    try:
        candidate_resolved.relative_to(root_resolved)
    except ValueError as exc:
        raise ValueError(f"Output path must remain beneath evidence root: {candidate}") from exc
    return candidate_resolved


def collect_entries(root: Path, output: Path) -> list[dict[str, object]]:
    root_resolved = root.resolve()
    output_resolved = _resolve_within(root, output)

    entries: list[dict[str, object]] = []
    for path in sorted(root_resolved.rglob("*.json")):
        if not path.is_file() or path.resolve() == output_resolved:
            continue
        data = path.read_bytes()
        entries.append(
            {
                "path": path.relative_to(root_resolved).as_posix(),
                "sizeBytes": len(data),
                "sha256": hashlib.sha256(data).hexdigest(),
            }
        )

    if not entries:
        raise ValueError(f"No dependency-evidence JSON files found under: {root}")

    return entries


def create_manifest(root: Path, output: Path) -> dict[str, object]:
    root_resolved = root.resolve()
    output_resolved = _resolve_within(root, output)
    entries = collect_entries(root_resolved, output_resolved)
    payload: dict[str, object] = {
        "schemaVersion": 1,
        "fileCount": len(entries),
        "files": entries,
    }
    output_resolved.parent.mkdir(parents=True, exist_ok=True)
    output_resolved.write_text(
        json.dumps(payload, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    return payload


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Create a deterministic SHA-256 manifest for dependency audit JSON files."
    )
    parser.add_argument("root", type=Path, help="Directory containing dependency JSON reports.")
    parser.add_argument("output", type=Path, help="Manifest JSON path beneath the evidence root.")
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(sys.argv[1:] if argv is None else argv)
    try:
        payload = create_manifest(args.root, args.output)
    except (OSError, ValueError) as exc:
        print(f"Dependency evidence manifest generation failed: {exc}", file=sys.stderr)
        return 2

    print(
        f"Dependency evidence manifest created: {args.output} "
        f"({payload['fileCount']} report file(s))."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
