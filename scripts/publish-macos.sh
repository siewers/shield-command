#!/bin/bash
set -euo pipefail

# Publish Shield Commander as a macOS .app bundle
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
PROJECT="$REPO_ROOT/src/ShieldCommander.UI/ShieldCommander.UI.csproj"
APP_NAME="Shield Commander"
BUNDLE_ID="com.shieldcommand.app"
OUTPUT_DIR="$REPO_ROOT/publish"
APP_BUNDLE="$OUTPUT_DIR/$APP_NAME.app"
RID="${1:-osx-arm64}"

echo "Publishing for $RID..."

# Clean previous output
rm -rf "$APP_BUNDLE"
mkdir -p "$OUTPUT_DIR"

# Publish the app
dotnet publish "$PROJECT" \
    -c Release \
    -r "$RID" \
    --self-contained true \
    -o "$OUTPUT_DIR/bin"

# Create .app bundle structure
mkdir -p "$APP_BUNDLE/Contents/MacOS"
mkdir -p "$APP_BUNDLE/Contents/Resources"

# Copy published output
cp -R "$OUTPUT_DIR/bin/"* "$APP_BUNDLE/Contents/MacOS/"

# Copy icon
cp "$REPO_ROOT/src/ShieldCommander.UI/Assets/app-icon.icns" \
   "$APP_BUNDLE/Contents/Resources/app-icon.icns"

# Copy and populate Info.plist template
VERSION="$(date +%Y.%-m.%-d)"
cp "$REPO_ROOT/src/ShieldCommander.UI/Info.plist" "$APP_BUNDLE/Contents/Info.plist"
sed -i '' "s/__VERSION__/${VERSION}/g; s/__YEAR__/$(date +%Y)/g" "$APP_BUNDLE/Contents/Info.plist"

# Clean up intermediate output
rm -rf "$OUTPUT_DIR/bin"

echo ""
echo "Done! App bundle created at:"
echo "  $APP_BUNDLE"
echo ""
echo "Run with:"
echo "  open \"$APP_BUNDLE\""
