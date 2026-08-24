#!/usr/bin/env python3
from __future__ import annotations

import argparse
import re
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path

WINDOWS_NS = "http://schemas.microsoft.com/appx/manifest/foundation/windows10"
SEMVER_RE = re.compile(r"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$")
MAX_ANDROID_VERSION_CODE = 2_100_000_000


class VersionAlignmentError(RuntimeError):
    pass


@dataclass(frozen=True)
class VersionState:
    display_version: str
    build_version: int
    share_display_version: str
    share_build_version: int
    desktop_version: str
    windows_version: str


def fail(message: str) -> None:
    raise VersionAlignmentError(message)


def project_value(path: Path, name: str) -> str:
    if not path.is_file():
        fail(f"Missing project file: {path}")
    root = ET.parse(path).getroot()
    values = [
        (node.text or "").strip()
        for node in root.findall(f".//{name}")
        if (node.text or "").strip()
    ]
    if not values:
        fail(f"{path.name} is missing {name}")
    if len(set(values)) != 1:
        fail(f"{path.name} contains conflicting {name} values: {values}")
    return values[0]


def parse_build(value: str, label: str) -> int:
    if not value.isdigit():
        fail(f"{label} must be a positive decimal integer")
    number = int(value)
    if number <= 0 or number > MAX_ANDROID_VERSION_CODE:
        fail(f"{label} must be between 1 and {MAX_ANDROID_VERSION_CODE}")
    return number


def parse_semver(version: str) -> tuple[int, int, int]:
    match = SEMVER_RE.fullmatch(version)
    if match is None:
        fail(f"Display version must use canonical major.minor.patch form: {version}")
    parts = tuple(int(group) for group in match.groups())
    if parts[1] > 99 or parts[2] > 99:
        fail("Minor and patch components must be <= 99 for the release build-code convention")
    return parts


def expected_build_for(version: str) -> int:
    major, minor, patch = parse_semver(version)
    build = major * 10_000 + minor * 100 + patch
    if build <= 0 or build > MAX_ANDROID_VERSION_CODE:
        fail(f"Derived build version {build} is outside the supported Android version-code range")
    return build


def windows_identity_version(path: Path) -> str:
    if not path.is_file():
        fail(f"Missing Windows package manifest: {path}")
    package = ET.parse(path).getroot()
    identity = package.find(f"{{{WINDOWS_NS}}}Identity")
    if identity is None:
        fail("Windows Package.appxmanifest is missing Identity")
    version = (identity.get("Version") or "").strip()
    if not version:
        fail("Windows Package.appxmanifest Identity is missing Version")
    return version


def read_state(root: Path) -> VersionState:
    app = root / "src/SwiftDrop.App/SwiftDrop.App.csproj"
    share = root / "src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj"
    desktop = root / "src/SwiftDrop.Desktop/SwiftDrop.Desktop.csproj"
    windows = root / "src/SwiftDrop.App/Platforms/Windows/Package.appxmanifest"

    return VersionState(
        display_version=project_value(app, "ApplicationDisplayVersion"),
        build_version=parse_build(project_value(app, "ApplicationVersion"), "SwiftDrop.App ApplicationVersion"),
        share_display_version=project_value(share, "ApplicationDisplayVersion"),
        share_build_version=parse_build(project_value(share, "ApplicationVersion"), "Share Extension ApplicationVersion"),
        desktop_version=project_value(desktop, "Version"),
        windows_version=windows_identity_version(windows),
    )


def validate(root: Path, expected_version: str | None = None, expected_build: int | None = None) -> VersionState:
    state = read_state(root)
    derived_build = expected_build_for(state.display_version)

    if state.share_display_version != state.display_version:
        fail("MAUI app and iOS Share Extension display versions must match")
    if state.desktop_version != state.display_version:
        fail("MAUI app and desktop host versions must match")
    if state.share_build_version != state.build_version:
        fail("MAUI app and iOS Share Extension build versions must match")
    if state.build_version != derived_build:
        fail(
            f"ApplicationVersion must follow major*10000 + minor*100 + patch: "
            f"expected {derived_build}, found {state.build_version}"
        )

    expected_windows = f"{state.display_version}.0"
    if state.windows_version != expected_windows:
        fail(f"Windows package version must be {expected_windows}, found {state.windows_version}")

    if expected_version is not None and state.display_version != expected_version:
        fail(f"Expected release version {expected_version}, found {state.display_version}")
    if expected_build is not None and state.build_version != expected_build:
        fail(f"Expected release build {expected_build}, found {state.build_version}")

    return state


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate SwiftDrop release-version alignment across maintained package surfaces.")
    parser.add_argument(
        "--root",
        type=Path,
        default=Path(__file__).resolve().parents[1],
        help="Repository root (defaults to this script's repository).",
    )
    parser.add_argument("--expected-version", help="Optional exact major.minor.patch release version to require.")
    parser.add_argument("--expected-build", type=int, help="Optional exact integer application build to require.")
    args = parser.parse_args()

    try:
        state = validate(args.root.resolve(), args.expected_version, args.expected_build)
    except (ET.ParseError, OSError, ValueError, VersionAlignmentError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1

    print(
        "Release versions aligned: "
        f"display={state.display_version}, build={state.build_version}, windows={state.windows_version}."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
