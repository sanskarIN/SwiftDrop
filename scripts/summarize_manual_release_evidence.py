#!/usr/bin/env python3
"""Summarize SwiftDrop manual release evidence without weakening validation."""

from __future__ import annotations

import argparse
import json
import sys
from collections import Counter
from pathlib import Path
from typing import Any

from validate_manual_release_evidence import VALID_STATUSES, validate_document


def summarize_document(document: dict[str, Any]) -> dict[str, Any]:
    validate_document(document)

    case_counts = Counter({status: 0 for status in sorted(VALID_STATUSES)})
    group_counts = Counter({status: 0 for status in sorted(VALID_STATUSES)})
    groups: list[dict[str, Any]] = []
    remaining: list[dict[str, str]] = []

    for group in document["groups"]:
        group_counts[group["status"]] += 1
        local_counts = Counter({status: 0 for status in sorted(VALID_STATUSES)})
        for case in group["cases"]:
            case_counts[case["status"]] += 1
            local_counts[case["status"]] += 1
            if case["status"] != "passed":
                remaining.append(
                    {
                        "group": group["id"],
                        "case": case["id"],
                        "status": case["status"],
                    }
                )

        groups.append(
            {
                "id": group["id"],
                "status": group["status"],
                "case_counts": dict(sorted(local_counts.items())),
            }
        )

    total_cases = sum(case_counts.values())
    passed_cases = case_counts["passed"]
    return {
        "candidate": dict(document["candidate"]),
        "complete": passed_cases == total_cases,
        "total_groups": len(document["groups"]),
        "total_cases": total_cases,
        "passed_cases": passed_cases,
        "remaining_cases": total_cases - passed_cases,
        "case_counts": dict(sorted(case_counts.items())),
        "group_counts": dict(sorted(group_counts.items())),
        "groups": groups,
        "remaining": remaining,
    }


def render_text(summary: dict[str, Any]) -> str:
    candidate = summary["candidate"]
    lines = [
        f"candidate: {candidate['version']} @ {candidate['commit']}",
        f"cases: {summary['passed_cases']}/{summary['total_cases']} passed; {summary['remaining_cases']} remaining",
        f"complete: {'yes' if summary['complete'] else 'no'}",
        "groups:",
    ]
    for group in summary["groups"]:
        counts = ", ".join(
            f"{status}={count}"
            for status, count in group["case_counts"].items()
            if count
        )
        lines.append(f"  - {group['id']}: {group['status']} ({counts})")
    return "\n".join(lines)


def render_remaining(summary: dict[str, Any]) -> str:
    remaining = summary["remaining"]
    if not remaining:
        return "all required manual release-evidence cases are passed"
    return "\n".join(
        f"{item['group']}/{item['case']}: {item['status']}"
        for item in remaining
    )


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("path", type=Path, help="manual release-evidence JSON document")
    output = parser.add_mutually_exclusive_group()
    output.add_argument("--json", action="store_true", help="emit the summary as JSON")
    output.add_argument(
        "--remaining-only",
        action="store_true",
        help="list only required cases that are not yet passed",
    )
    return parser.parse_args(argv)


def main(argv: list[str]) -> int:
    args = parse_args(argv[1:])
    try:
        document = json.loads(args.path.read_text(encoding="utf-8"))
        summary = summarize_document(document)
    except (OSError, json.JSONDecodeError, ValueError) as exc:
        print(f"could not summarize manual release evidence: {exc}", file=sys.stderr)
        return 1

    if args.json:
        print(json.dumps(summary, indent=2, ensure_ascii=False))
    elif args.remaining_only:
        print(render_remaining(summary))
    else:
        print(render_text(summary))
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
