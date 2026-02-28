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

# Create Info.plist
cat > "$APP_BUNDLE/Contents/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundleDisplayName</key>
    <string>$APP_NAME</string>
    <key>CFBundleIdentifier</key>
    <string>$BUNDLE_ID</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0.0</string>
    <key>CFBundleExecutable</key>
    <string>ShieldCommander</string>
    <key>CFBundleIconFile</key>
    <string>app-icon</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>LSMinimumSystemVersion</key>
    <string>12.0</string>
    <key>NSHumanReadableCopyright</key>
    <string>Copyright © $(date +%Y) Siewers Software. All rights reserved.</string>
</dict>
</plist>
EOF

# Clean up intermediate output
rm -rf "$OUTPUT_DIR/bin"

echo ""
echo "Done! App bundle created at:"
echo "  $APP_BUNDLE"
echo ""
echo "Run with:"
echo "  open \"$APP_BUNDLE\""
