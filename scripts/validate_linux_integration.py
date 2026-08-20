#!/usr/bin/env python3
from __future__ import annotations

import pathlib
import sys
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[1]


def fail(message: str) -> None:
    print(f"linux integration validation failed: {message}", file=sys.stderr)
    raise SystemExit(1)


def read(relative: str) -> str:
    path = ROOT / relative
    if not path.is_file():
        fail(f"missing required file: {relative}")
    return path.read_text(encoding="utf-8")


def require(text: str, needle: str, source: str) -> None:
    if needle not in text:
        fail(f"{source} is missing required contract: {needle}")


def validate_project() -> None:
    relative = "src/SwiftDrop.Desktop/SwiftDrop.Desktop.csproj"
    text = read(relative)
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        fail(f"{relative} is invalid XML: {exc}")

    values = {element.tag: (element.text or "").strip() for element in root.iter()}
    if values.get("TargetFramework") != "net10.0":
        fail("desktop project must target net10.0")
    if values.get("OutputType") != "WinExe":
        fail("desktop project must remain a GUI executable")

    references = [
        element.attrib.get("Include", "")
        for element in root.iter("ProjectReference")
    ]
    if "../SwiftDrop.Core/SwiftDrop.Core.csproj" not in references:
        fail("desktop project must reference SwiftDrop.Core")

    package_refs = {
        element.attrib.get("Include", ""): element.attrib.get("Version", "")
        for element in root.iter("PackageReference")
    }
    if "Avalonia.Desktop" not in package_refs:
        fail("desktop project must reference Avalonia.Desktop")

    runtime_ids = values.get("RuntimeIdentifiers", "").split(";")
    for rid in ("linux-x64", "linux-arm64"):
        if rid not in runtime_ids:
            fail(f"desktop project is missing runtime identifier {rid}")


def validate_solution() -> None:
    text = read("SwiftDrop.slnx")
    require(text, 'Project Path="src/SwiftDrop.Desktop/SwiftDrop.Desktop.csproj"', "SwiftDrop.slnx")


def validate_desktop_entry() -> None:
    relative = "packaging/linux/in.sanskar.swiftdrop.desktop"
    text = read(relative)
    for contract in (
        "Type=Application",
        "Name=SwiftDrop",
        "Exec=swiftdrop %u",
        "Terminal=false",
        "MimeType=x-scheme-handler/swiftdrop;",
    ):
        require(text, contract, relative)


def validate_launcher_source() -> None:
    launch = read("src/SwiftDrop.Desktop/MainWindow.Launch.cs")
    require(launch, 'StartsWith("swiftdrop://pair"', "MainWindow.Launch.cs")
    require(launch, "_pairing.DecodePairingLink", "MainWindow.Launch.cs")

    app = read("src/SwiftDrop.Desktop/App.axaml.cs")
    require(app, "ApplyLaunchArguments", "App.axaml.cs")


def validate_packaging() -> None:
    relative = "scripts/publish-linux.sh"
    text = read(relative)
    for contract in (
        "linux-x64|linux-arm64",
        "--self-contained true",
        "in.sanskar.swiftdrop.desktop",
        "x-scheme-handler/swiftdrop",
        "SwiftDrop.Desktop",
    ):
        require(text, contract, relative)


def validate_workflow() -> None:
    relative = ".github/workflows/desktop-linux.yml"
    text = read(relative)
    for contract in (
        "linux-x64",
        "linux-arm64",
        "SwiftDrop.Desktop.csproj",
        "validate_nuget_vulnerability_report.py",
    ):
        require(text, contract, relative)


def main() -> None:
    validate_project()
    validate_solution()
    validate_desktop_entry()
    validate_launcher_source()
    validate_packaging()
    validate_workflow()
    print("Linux desktop integration validation passed.")


if __name__ == "__main__":
    main()
