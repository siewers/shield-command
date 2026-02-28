#!/bin/bash
set -euo pipefail

# Publish Shield Commander as a macOS .app bundle (and optionally a .dmg)
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
PROJECT="$REPO_ROOT/src/ShieldCommander.UI/ShieldCommander.UI.csproj"
APP_NAME="Shield Commander"
OUTPUT_DIR="$REPO_ROOT/publish"
APP_BUNDLE="$OUTPUT_DIR/$APP_NAME.app"
RID="${1:-osx-arm64}"
VERSION="${2:-$(date +%Y.%-m.%-d)}"

echo "Publishing for $RID (version $VERSION)..."

# Clean previous output
rm -rf "$APP_BUNDLE"
mkdir -p "$OUTPUT_DIR"

# Publish the app
dotnet publish "$PROJECT" \
    -c Release \
    -r "$RID" \
    --self-contained true \
    -o "$OUTPUT_DIR/bin" \
    -p:Version="$VERSION"

# Create .app bundle structure
mkdir -p "$APP_BUNDLE/Contents/MacOS"
mkdir -p "$APP_BUNDLE/Contents/Resources"

# Copy published output
cp -R "$OUTPUT_DIR/bin/"* "$APP_BUNDLE/Contents/MacOS/"

# Copy icon
cp "$REPO_ROOT/src/ShieldCommander.UI/Assets/app-icon.icns" \
   "$APP_BUNDLE/Contents/Resources/app-icon.icns"

# Copy and populate Info.plist template
cp "$REPO_ROOT/src/ShieldCommander.UI/Info.plist" "$APP_BUNDLE/Contents/Info.plist"
sed -i '' "s/__VERSION__/${VERSION}/g; s/__YEAR__/$(date +%Y)/g" "$APP_BUNDLE/Contents/Info.plist"

# Clean up intermediate output
rm -rf "$OUTPUT_DIR/bin"

# Create DMG if requested
if [[ "${3:-}" == "--dmg" ]]; then
    DMG_PATH="${4:-$OUTPUT_DIR/ShieldCommander-macos-${RID#osx-}.dmg}"
    rm -f "$DMG_PATH"

    if command -v create-dmg &>/dev/null; then
        create-dmg \
            --volname "$APP_NAME" \
            --window-pos 200 120 \
            --window-size 600 400 \
            --icon-size 100 \
            --icon "$APP_NAME.app" 150 185 \
            --app-drop-link 450 185 \
            --no-internet-enable \
            "$DMG_PATH" \
            "$APP_BUNDLE"
    else
        echo "create-dmg not found, falling back to hdiutil"
        hdiutil create -volname "$APP_NAME" \
            -srcfolder "$APP_BUNDLE" \
            -ov -format UDZO \
            "$DMG_PATH"
    fi
    echo "DMG created at: $DMG_PATH"
else
    echo ""
    echo "Done! App bundle created at:"
    echo "  $APP_BUNDLE"
    echo ""
    echo "Run with:"
    echo "  open \"$APP_BUNDLE\""
fi
