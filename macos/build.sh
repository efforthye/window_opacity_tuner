#!/usr/bin/env bash
#
# Builds "Window Opacity Tuner.app" for macOS 13+ into macos/dist/.
#
#   ./build.sh             native arch (Apple silicon or Intel)
#   ./build.sh --universal arm64 + x86_64 in one binary
#   ./build.sh --install   also copy the app to ~/Applications
#
set -euo pipefail

cd "$(dirname "$0")"

UNIVERSAL=0
INSTALL=0
while [[ $# -gt 0 ]]; do
  case "$1" in
    --universal) UNIVERSAL=1; shift ;;
    --install) INSTALL=1; shift ;;
    -h|--help) sed -n '2,8p' "$0"; exit 0 ;;
    *) echo "unknown option: $1" >&2; exit 2 ;;
  esac
done

NAME="Window Opacity Tuner"
APP="dist/$NAME.app"
BUNDLE_ID="com.efforthye.WindowOpacityTuner"
VERSION="1.0.0"

if [[ $UNIVERSAL -eq 1 ]]; then
  swift build -c release --arch arm64 --arch x86_64
  BIN=".build/apple/Products/Release/WindowOpacityTuner"
else
  swift build -c release
  BIN="$(swift build -c release --show-bin-path)/WindowOpacityTuner"
fi

rm -rf "$APP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp "$BIN" "$APP/Contents/MacOS/WindowOpacityTuner"
cp AppIcon.icns "$APP/Contents/Resources/AppIcon.icns"

cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key><string>$NAME</string>
  <key>CFBundleDisplayName</key><string>$NAME</string>
  <key>CFBundleIdentifier</key><string>$BUNDLE_ID</string>
  <key>CFBundleExecutable</key><string>WindowOpacityTuner</string>
  <key>CFBundleIconFile</key><string>AppIcon</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>$VERSION</string>
  <key>CFBundleVersion</key><string>$VERSION</string>
  <key>LSMinimumSystemVersion</key><string>13.0</string>
  <key>NSHighResolutionCapable</key><true/>
  <key>NSScreenCaptureUsageDescription</key>
  <string>Draws what is behind a window on top of it to make the window look translucent.</string>
</dict>
</plist>
PLIST

# Ad-hoc signature, enough to run locally. macOS checks the Screen Recording
# grant against the designated requirement; an ad-hoc one defaults to the exact
# binary hash, which would drop the grant on every rebuild. Pinning it to the
# bundle id keeps the permission across rebuilds.
codesign --force --deep --sign - -r="designated => identifier \"$BUNDLE_ID\"" "$APP"

if [[ $INSTALL -eq 1 ]]; then
  mkdir -p "$HOME/Applications"
  rm -rf "$HOME/Applications/$NAME.app"
  cp -R "$APP" "$HOME/Applications/"
  echo "Installed to ~/Applications/$NAME.app"
fi

echo
echo "Done: macos/$APP"
