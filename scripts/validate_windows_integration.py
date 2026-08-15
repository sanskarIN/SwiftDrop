#!/usr/bin/env python3
from __future__ import annotations

import argparse
import re
import sys
import uuid
import xml.etree.ElementTree as ET
from pathlib import Path

FOUNDATION_NS = "http://schemas.microsoft.com/appx/manifest/foundation/windows10"
UAP_NS = "http://schemas.microsoft.com/appx/manifest/uap/windows10"
DESKTOP_NS = "http://schemas.microsoft.com/appx/manifest/desktop/windows10"
COM_NS = "http://schemas.microsoft.com/appx/manifest/com/windows10"


def fail(message: str) -> None:
    raise RuntimeError(message)


def qname(namespace: str, local_name: str) -> str:
    return f"{{{namespace}}}{local_name}"


def parse_manifest(root: Path) -> ET.Element:
    path = root / "src/SwiftDrop.App/Platforms/Windows/Package.appxmanifest"
    if not path.is_file():
        fail("Missing Windows Package.appxmanifest")
    package = ET.parse(path).getroot()
    if package.tag != qname(FOUNDATION_NS, "Package"):
        fail("Windows manifest root is not the expected Package element")
    return package


def single(nodes: list[ET.Element], label: str) -> ET.Element:
    if len(nodes) != 1:
        fail(f"Expected exactly one {label}, found {len(nodes)}")
    return nodes[0]


def assert_protocol_and_capabilities(package: ET.Element) -> None:
    applications = package.findall(f".//{qname(FOUNDATION_NS, 'Application')}")
    application = single(applications, "Windows Application")

    protocols = [
        node
        for node in application.findall(f".//{qname(UAP_NS, 'Protocol')}")
        if node.get("Name") == "swiftdrop"
    ]
    single(protocols, "swiftdrop protocol registration")

    capability_names = {
        node.get("Name")
        for node in package.findall(f".//{qname(FOUNDATION_NS, 'Capability')}")
    }
    if "privateNetworkClientServer" not in capability_names:
        fail("Windows manifest must retain privateNetworkClientServer capability")
    if "internetClient" in capability_names:
        fail("Protocol-v1 Windows manifest must not add internetClient capability")


def assert_notification_registration(package: ET.Element) -> str:
    desktop_extensions = [
        node
        for node in package.findall(f".//{qname(DESKTOP_NS, 'Extension')}")
        if node.get("Category") == "windows.toastNotificationActivation"
    ]
    desktop_extension = single(desktop_extensions, "toast notification activation extension")
    activator = single(
        desktop_extension.findall(qname(DESKTOP_NS, "ToastNotificationActivation")),
        "toast notification activator",
    )
    toast_clsid = activator.get("ToastActivatorCLSID") or ""
    try:
        normalized_toast_clsid = str(uuid.UUID(toast_clsid)).upper()
    except ValueError as exc:
        raise RuntimeError("ToastActivatorCLSID must be a valid GUID") from exc

    com_extensions = [
        node
        for node in package.findall(f".//{qname(COM_NS, 'Extension')}")
        if node.get("Category") == "windows.comServer"
    ]
    com_extension = single(com_extensions, "COM server extension")
    com_server = single(com_extension.findall(qname(COM_NS, "ComServer")), "COM server")
    exe_server = single(com_server.findall(qname(COM_NS, "ExeServer")), "notification ExeServer")
    if exe_server.get("Executable") != "$targetnametoken$.exe":
        fail("Notification ExeServer must use $targetnametoken$.exe")
    if exe_server.get("Arguments") != "----AppNotificationActivated:":
        fail("Notification ExeServer activation arguments changed unexpectedly")

    classes = exe_server.findall(qname(COM_NS, "Class"))
    com_class = single(classes, "notification COM class")
    class_id = com_class.get("Id") or ""
    try:
        normalized_class_id = str(uuid.UUID(class_id)).upper()
    except ValueError as exc:
        raise RuntimeError("Notification COM class Id must be a valid GUID") from exc
    if normalized_class_id != normalized_toast_clsid:
        fail("ToastActivatorCLSID and notification COM class Id must match")
    return normalized_class_id


def read_resource_values(path: Path) -> dict[str, str]:
    if not path.is_file():
        fail(f"Missing notification resource catalog: {path.name}")
    result: dict[str, str] = {}
    root = ET.parse(path).getroot()
    for data in root.findall("data"):
        key = data.get("name")
        value = data.find("value")
        if key and value is not None:
            result[key] = value.text or ""
    return result


def assert_generic_notification_text(root: Path) -> None:
    required = ("TransferCompletedNotification", "TransferFailedNotification")
    catalogs = (
        root / "src/SwiftDrop.App/Resources/Strings/PlatformRuntimeStrings.resx",
        root / "src/SwiftDrop.App/Resources/Strings/PlatformRuntimeStrings.hi.resx",
    )
    for catalog in catalogs:
        values = read_resource_values(catalog)
        for key in required:
            value = values.get(key)
            if value is None or not value.strip():
                fail(f"{catalog.name} is missing non-empty {key}")
            if re.search(r"\{\d+[^}]*\}", value):
                fail(f"{catalog.name} {key} must remain generic and placeholder-free")


def assert_notification_source(root: Path) -> None:
    path = root / "src/SwiftDrop.App/Services/TransferNotificationService.cs"
    if not path.is_file():
        fail("Missing TransferNotificationService.cs")
    source = path.read_text(encoding="utf-8")
    required_fragments = (
        "AppNotificationManager.Default",
        "manager.Register();",
        "_windowsManager!.Show(notification);",
        'AppText.Get(success ? "TransferCompletedNotification" : "TransferFailedNotification")',
    )
    for fragment in required_fragments:
        if fragment not in source:
            fail(f"Windows notification source contract missing: {fragment}")


def validate(root: Path) -> None:
    package = parse_manifest(root)
    assert_protocol_and_capabilities(package)
    assert_notification_registration(package)
    assert_generic_notification_text(root)
    assert_notification_source(root)


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate SwiftDrop Windows notification/package integration.")
    parser.add_argument(
        "--root",
        type=Path,
        default=Path(__file__).resolve().parents[1],
        help="Repository root (defaults to this script's repository).",
    )
    args = parser.parse_args()
    root = args.root.resolve()

    try:
        validate(root)
    except (ET.ParseError, OSError, RuntimeError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1

    print("Windows protocol, private-network capability, app-notification registration, and generic notification text are internally consistent.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
