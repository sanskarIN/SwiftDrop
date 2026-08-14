#!/usr/bin/env python3
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
APP_GROUP = "group.in.sanskar.swiftdrop"
APP_ID = "in.sanskar.swiftdrop"
EXTENSION_ID = "in.sanskar.swiftdrop.share"
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


def assert_mac_sandbox(path: Path) -> None:
    values = plist_dict(path)
    node = values.get("com.apple.security.app-sandbox")
    if not isinstance(node, ET.Element) or node.tag != "true":
        fail(f"Mac target is missing app sandbox entitlement: {path.relative_to(ROOT)}")


def nested_dict_pairs(node: ET.Element, label: str) -> dict[str, ET.Element]:
    if node.tag != "dict":
        fail(f"{label} must be a plist dictionary")
    children = list(node)
    if len(children) % 2 != 0:
        fail(f"Malformed {label} dictionary")
    pairs: dict[str, ET.Element] = {}
    for index in range(0, len(children), 2):
        if children[index].tag != "key" or children[index].text is None:
            fail(f"Malformed {label} key/value sequence")
        key = children[index].text
        if key in pairs:
            fail(f"Duplicate {label} key: {key}")
        pairs[key] = children[index + 1]
    return pairs


def assert_share_info(path: Path) -> None:
    values = plist_dict(path)
    extension = values.get("NSExtension")
    if not isinstance(extension, ET.Element):
        fail("Share Extension Info.plist is missing NSExtension dictionary")
    pairs = nested_dict_pairs(extension, "NSExtension")

    point = pairs.get("NSExtensionPointIdentifier")
    principal = pairs.get("NSExtensionPrincipalClass")
    if point is None or point.text != "com.apple.share-services":
        fail("Share Extension point identifier must be com.apple.share-services")
    if principal is None or principal.text != "ShareViewController":
        fail("Share Extension principal class must be ShareViewController")

    attributes = pairs.get("NSExtensionAttributes")
    if attributes is None:
        fail("Share Extension is missing NSExtensionAttributes")
    attribute_pairs = nested_dict_pairs(attributes, "NSExtensionAttributes")
    activation = attribute_pairs.get("NSExtensionActivationRule")
    if activation is None:
        fail("Share Extension is missing activation rule")
    activation_pairs = nested_dict_pairs(activation, "NSExtensionActivationRule")
    if activation_pairs.get("NSExtensionActivationSupportsText") is None or activation_pairs["NSExtensionActivationSupportsText"].tag != "true":
        fail("Share Extension must accept explicit text shares")
    file_max = activation_pairs.get("NSExtensionActivationSupportsFileWithMaxCount")
    if file_max is None or file_max.tag != "integer" or file_max.text != "64":
        fail("Share Extension file activation maximum must remain 64")


def first_project_value(root: ET.Element, name: str) -> str:
    for node in root.findall(f".//{name}"):
        if node.text and node.text.strip():
            return node.text.strip()
    fail(f"Project is missing {name}")
    raise AssertionError("unreachable")


def assert_codesign_file(root: ET.Element, expected: str) -> None:
    values = [(node.text or "").strip() for node in root.findall(".//CodesignEntitlements")]
    if expected not in values:
        fail(f"Project is not wired to codesign entitlements: {expected}")


def assert_project_configuration() -> None:
    app_path = ROOT / "src/SwiftDrop.App/SwiftDrop.App.csproj"
    extension_path = ROOT / "src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj"
    app_root = ET.parse(app_path).getroot()
    extension_root = ET.parse(extension_path).getroot()

    if first_project_value(app_root, "ApplicationId") != APP_ID:
        fail("SwiftDrop.App ApplicationId changed unexpectedly")
    if first_project_value(extension_root, "ApplicationId") != EXTENSION_ID:
        fail("Share Extension ApplicationId changed unexpectedly")
    if first_project_value(app_root, "ApplicationDisplayVersion") != first_project_value(extension_root, "ApplicationDisplayVersion"):
        fail("App and Share Extension display versions must match")
    if first_project_value(app_root, "ApplicationVersion") != first_project_value(extension_root, "ApplicationVersion"):
        fail("App and Share Extension build versions must match")

    app_refs = [node for node in app_root.findall(".//ProjectReference") if node.get("Include") == EXTENSION_PROJECT]
    if len(app_refs) != 1:
        fail("SwiftDrop.App must reference the Share Extension exactly once")
    marker = app_refs[0].find("IsAppExtension")
    if marker is None or (marker.text or "").strip().lower() != "true":
        fail("SwiftDrop.App Share Extension reference must set IsAppExtension=true")

    if first_project_value(extension_root, "TargetFramework") != "net10.0-ios":
        fail("Share Extension must target net10.0-ios only")
    if first_project_value(extension_root, "IsAppExtension").lower() != "true":
        fail("Share Extension project must set IsAppExtension=true")
    if first_project_value(extension_root, "InfoPlist") != "Info.plist":
        fail("Share Extension project must use its dedicated Info.plist")

    assert_codesign_file(app_root, "Platforms/iOS/Entitlements.plist")
    assert_codesign_file(app_root, "Platforms/MacCatalyst/Entitlements.plist")
    assert_codesign_file(extension_root, "Platforms/iOS/Entitlements.plist")

    solution = (ROOT / "SwiftDrop.slnx").read_text(encoding="utf-8")
    if "src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj" not in solution:
        fail("SwiftDrop.slnx does not include the Share Extension project")

    constants = (ROOT / "src/SwiftDrop.Core/Protocol/ExternalSharePackageConstants.cs").read_text(encoding="utf-8")
    match = re.search(r'AppleAppGroupId\s*=\s*"([^"]+)"', constants)
    if match is None or match.group(1) != APP_GROUP:
        fail("Core AppleAppGroupId constant does not match entitlements")


def main() -> int:
    errors: list[str] = []
    try:
        paths = (
            "src/SwiftDrop.App/Platforms/iOS/Entitlements.plist",
            "src/SwiftDrop.App/Platforms/MacCatalyst/Entitlements.plist",
            "src/SwiftDrop.ShareExtension/Platforms/iOS/Entitlements.plist",
        )
        for relative in paths:
            assert_app_group(ROOT / relative)
        assert_mac_sandbox(ROOT / "src/SwiftDrop.App/Platforms/MacCatalyst/Entitlements.plist")
        assert_share_info(ROOT / "src/SwiftDrop.ShareExtension/Info.plist")
        assert_project_configuration()
    except (ET.ParseError, OSError, RuntimeError) as exc:
        errors.append(str(exc))

    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    print("Apple App Group, versions, entitlements, and iOS Share Extension configuration are internally consistent.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
