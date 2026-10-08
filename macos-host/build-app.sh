#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
if [[ "$(uname -m)" != arm64 ]]; then echo "Apple Silicon required" >&2; exit 1; fi
# Newer SwiftPM defaults to Swift Build. Use the stable native backend and
# a separate cache so products from different backends are never mixed.
xcrun swift build --build-system native --scratch-path .build-native -c release
bin_dir="$(xcrun swift build --build-system native --scratch-path .build-native -c release --show-bin-path)"
app="$PWD/build/RemoteMacLAN.app"
mkdir -p "$app/Contents/MacOS"
cp "$bin_dir/RemoteMacLAN" "$app/Contents/MacOS/RemoteMacLAN"
cp Info.plist "$app/Contents/Info.plist"
identity="${SIGN_IDENTITY:-}"
identity_file="$PWD/.signing-identity"
if [[ -z "$identity" && -f "$identity_file" ]]; then identity="$(cat "$identity_file")"; fi
if [[ -z "$identity" ]]; then
  identity="$(security find-identity -v -p codesigning | awk '/"Developer ID Application:|"Apple Development:/ { print $2; exit }')"
fi
if [[ -z "$identity" ]]; then identity="-"; fi
codesign --force --sign "$identity" --identifier com.remotemaclan.host "$app"
codesign --verify --strict "$app"
if [[ "$identity" != "-" ]]; then
  printf '%s\n' "$identity" > "$identity_file"
  echo "Reusing a fixed local signing identity for future builds."
else
  echo "No developer signing certificate found: ad-hoc build; privacy permissions may need renewal after code updates." >&2
fi
echo "Built $app"
