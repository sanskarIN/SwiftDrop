#!/usr/bin/env bash
set -euo pipefail

RID="${1:-linux-x64}"
case "$RID" in
  linux-x64|linux-arm64) ;;
  *)
    echo "Unsupported Linux RID: $RID" >&2
    echo "Usage: bash scripts/publish-linux.sh [linux-x64|linux-arm64]" >&2
    exit 2
    ;;
esac

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT/src/SwiftDrop.Desktop/SwiftDrop.Desktop.csproj"
OUT="$ROOT/artifacts/linux/$RID/app"
PACKAGE_ROOT="$ROOT/artifacts/linux/$RID/package"
ARCHIVE="$ROOT/artifacts/linux/SwiftDrop-$RID.tar.gz"

rm -rf "$ROOT/artifacts/linux/$RID"
mkdir -p "$OUT" "$PACKAGE_ROOT/bin" "$PACKAGE_ROOT/share/applications" "$PACKAGE_ROOT/share/icons/hicolor/scalable/apps"

dotnet restore "$PROJECT" -r "$RID"
dotnet publish "$PROJECT" \
  -c Release \
  -r "$RID" \
  --self-contained true \
  --no-restore \
  -p:PublishTrimmed=false \
  -o "$OUT"

cp -a "$OUT/." "$PACKAGE_ROOT/bin/"
cp "$ROOT/packaging/linux/in.sanskar.swiftdrop.desktop" "$PACKAGE_ROOT/share/applications/in.sanskar.swiftdrop.desktop"
cp "$ROOT/src/SwiftDrop.App/Resources/AppIcon/appicon.svg" "$PACKAGE_ROOT/share/icons/hicolor/scalable/apps/swiftdrop.svg"

cat > "$PACKAGE_ROOT/install.sh" <<'INSTALL'
#!/usr/bin/env bash
set -euo pipefail

PREFIX="${XDG_DATA_HOME:-$HOME/.local/share}"
BIN_DIR="${HOME}/.local/bin"
APP_DIR="${PREFIX}/applications"
ICON_DIR="${PREFIX}/icons/hicolor/scalable/apps"
SOURCE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INSTALL_ROOT="${PREFIX}/swiftdrop"
EXECUTABLE="${INSTALL_ROOT}/SwiftDrop.Desktop"
DESKTOP_FILE="${APP_DIR}/in.sanskar.swiftdrop.desktop"

mkdir -p "$BIN_DIR" "$APP_DIR" "$ICON_DIR"
rm -rf "$INSTALL_ROOT"
mkdir -p "$INSTALL_ROOT"
cp -a "$SOURCE/bin/." "$INSTALL_ROOT/"
chmod +x "$EXECUTABLE"
ln -sfn "$EXECUTABLE" "$BIN_DIR/swiftdrop"

awk -v executable="$EXECUTABLE" '
  /^Exec=/ { print "Exec=\"" executable "\" %u"; next }
  { print }
' "$SOURCE/share/applications/in.sanskar.swiftdrop.desktop" > "$DESKTOP_FILE"
cp "$SOURCE/share/icons/hicolor/scalable/apps/swiftdrop.svg" "$ICON_DIR/swiftdrop.svg"

if command -v update-desktop-database >/dev/null 2>&1; then
  update-desktop-database "$APP_DIR" || true
fi
if command -v xdg-mime >/dev/null 2>&1; then
  xdg-mime default in.sanskar.swiftdrop.desktop x-scheme-handler/swiftdrop || true
fi
if command -v gtk-update-icon-cache >/dev/null 2>&1; then
  gtk-update-icon-cache -f -t "${PREFIX}/icons/hicolor" || true
fi

printf 'SwiftDrop installed at %s. CLI link: %s\n' "$EXECUTABLE" "$BIN_DIR/swiftdrop"
INSTALL
chmod +x "$PACKAGE_ROOT/install.sh"
chmod +x "$PACKAGE_ROOT/bin/SwiftDrop.Desktop"

tar -C "$PACKAGE_ROOT" -czf "$ARCHIVE" .

echo "Published: $OUT"
echo "Package:   $ARCHIVE"
echo "Install after extraction with: bash install.sh"
