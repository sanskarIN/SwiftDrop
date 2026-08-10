#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
PAIRS = [
    (
        ROOT / "src/SwiftDrop.App/Resources/Strings/AppStrings.resx",
        ROOT / "src/SwiftDrop.App/Resources/Strings/AppStrings.hi.resx",
    ),
    (
        ROOT / "src/SwiftDrop.App/Resources/Strings/MainStrings.resx",
        ROOT / "src/SwiftDrop.App/Resources/Strings/MainStrings.hi.resx",
    ),
    (
        ROOT / "src/SwiftDrop.App/Resources/Strings/DialogStrings.resx",
        ROOT / "src/SwiftDrop.App/Resources/Strings/DialogStrings.hi.resx",
    ),
    (
        ROOT / "src/SwiftDrop.App/Resources/Strings/MainRuntimeStrings.resx",
        ROOT / "src/SwiftDrop.App/Resources/Strings/MainRuntimeStrings.hi.resx",
    ),
    (
        ROOT / "src/SwiftDrop.App/Resources/Strings/PlatformRuntimeStrings.resx",
        ROOT / "src/SwiftDrop.App/Resources/Strings/PlatformRuntimeStrings.hi.resx",
    ),
]


def read_catalog(path: Path) -> dict[str, str]:
    if not path.is_file():
        raise RuntimeError(f"Missing localization catalog: {path.relative_to(ROOT)}")
    root = ET.parse(path).getroot()
    values: dict[str, str] = {}
    for item in root.findall("data"):
        key = (item.get("name") or "").strip()
        value_node = item.find("value")
        value = "" if value_node is None or value_node.text is None else value_node.text.strip()
        if not key:
            raise RuntimeError(f"Empty localization key in {path.relative_to(ROOT)}")
        if key in values:
            raise RuntimeError(f"Duplicate localization key {key!r} in {path.relative_to(ROOT)}")
        if not value:
            raise RuntimeError(f"Empty localization value for {key!r} in {path.relative_to(ROOT)}")
        values[key] = value
    if not values:
        raise RuntimeError(f"Localization catalog has no entries: {path.relative_to(ROOT)}")
    return values


def main() -> int:
    errors: list[str] = []
    for english_path, hindi_path in PAIRS:
        try:
            english = read_catalog(english_path)
            hindi = read_catalog(hindi_path)
        except (ET.ParseError, RuntimeError) as exc:
            errors.append(str(exc))
            continue

        missing_hindi = sorted(set(english) - set(hindi))
        extra_hindi = sorted(set(hindi) - set(english))
        if missing_hindi:
            errors.append(
                f"{hindi_path.relative_to(ROOT)} is missing keys: {', '.join(missing_hindi)}"
            )
        if extra_hindi:
            errors.append(
                f"{hindi_path.relative_to(ROOT)} has unmatched keys: {', '.join(extra_hindi)}"
            )

    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    print("Localization catalogs are well-formed and English/Hindi key sets match.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
