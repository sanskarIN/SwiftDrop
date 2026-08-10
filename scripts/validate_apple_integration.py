#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
APP_GROUP = "group.in.sanskar.swiftdrop"
EXTENSION_PROJECT = "../SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj"


def fail(message: str) -> None:
    raise RuntimeError(message)


def plist_dict(path: Path) -> dict[str, object]:
    if not path.is_file():
        fail(f"Missing plist: {path.relative_to(ROOT)}")
    root = ET.parse(path).getroot()
    dictionary = root.find("dict")
    if dictionary is None:
        fail(f"Missing top-level dict in {path.relative_to(ROOT)}")
    children = list(dictionary)
    result: dict[str, object] = {}
    index = 0
    while index < len(children):
        key = children[index]
        if key.tag != "key" or key.text is None or index + 1 >= len(children):
            fail(f"Malformed key/value sequence in {path.relative_to(ROOT)}")
        value = children[index + 1]
        result[key.text] = value
        index += 2
    return result


def plist_array_strings(node: ET.Element) -> list[str]:
    if node.tag != "array":
        return []
    return [(child.text or "") for child in node if child.tag == "string"]


def assert_app_group(path: Path) -> None:
    values = plist_dict(path)
    node = values.get("com.apple.security.application-groups")
    if not isinstance(node, ET.Element):
        fail(f"Missing App Group entitlement in {path.relative_to(ROOT)}")
    groups = plist_array_strings(node)
    if groups != [APP_GROUP]:
        fail(f"Unexpected App Group entitlement in {path.relative_to(ROOT)}: {groups}")


def assert_share_info(path: Path) -> None:
    values = plist_dict(path)
    extension = values.get("NSExtension")
    if not isinstance(extension, ET.Element) or extension.tag != "dict":
        fail("Share Extension Info.plist is missing NSExtension dictionary")

    children = list(extension)
    pairs: dict[str, ET.Element] = {}
    for index in range(0, len(children), 2):
        if index + 1 >= len(children) or children[index].tag != "key" or children[index].text is None:
            fail("Malformed NSExtension dictionary")
        pairs[children[index].text] = children[index + 1]

    point = pairs.get("NSExtensionPointIdentifier")
    principal = pairs.get("NSExtensionPrincipalClass")
    if point is None or point.text != "com.apple.share-services":
        fail("Share Extension point identifier must be com.apple.share-services")
    if principal is None or principal.text != "ShareViewController":
        fail("Share Extension principal class must be ShareViewController")


def assert_project_configuration() -> None:
    app_path = ROOT / "src/SwiftDrop.App/SwiftDrop.App.csproj"
    extension_path = ROOT / "src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj"
    app_root = ET.parse(app_path).getroot()
    extension_root = ET.parse(extension_path).getroot()

    app_refs = [node for node in app_root.findall(".//ProjectReference") if node.get("Include") == EXTENSION_PROJECT]
    if len(app_refs) != 1:
        fail("SwiftDrop.App must reference the Share Extension exactly once")
    marker = app_refs[0].find("IsAppExtension")
    if marker is None or (marker.text or "").strip().lower() != "true":
        fail("SwiftDrop.App Share Extension reference must set IsAppExtension=true")

    target_frameworks = [
        (node.text or "").strip()
        for node in extension_root.findall(".//TargetFrameworks")
        if node.text
    ]
    if not target_frameworks or "net10.0-ios" not in target_frameworks[0] or "net10.0-maccatalyst" not in target_frameworks[0]:
        fail("Share Extension must target net10.0-ios and net10.0-maccatalyst")

    flags = [(node.text or "").strip().lower() for node in extension_root.findall(".//IsAppExtension")]
    if "true" not in flags:
        fail("Share Extension project must set IsAppExtension=true")

    solution = (ROOT / "SwiftDrop.slnx").read_text(encoding="utf-8")
    if "src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj" not in solution:
        fail("SwiftDrop.slnx does not include the Share Extension project")


def main() -> int:
    errors: list[str] = []
    try:
        for relative in (
            "src/SwiftDrop.App/Platforms/iOS/Entitlements.plist",
            "src/SwiftDrop.App/Platforms/MacCatalyst/Entitlements.plist",
            "src/SwiftDrop.ShareExtension/Platforms/iOS/Entitlements.plist",
            "src/SwiftDrop.ShareExtension/Platforms/MacCatalyst/Entitlements.plist",
        ):
            assert_app_group(ROOT / relative)
        assert_share_info(ROOT / "src/SwiftDrop.ShareExtension/Info.plist")
        assert_project_configuration()
    except (ET.ParseError, OSError, RuntimeError) as exc:
        errors.append(str(exc))

    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    print("Apple App Group and Share Extension configuration is internally consistent.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
