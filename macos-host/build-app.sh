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
codesign --force --sign "${SIGN_IDENTITY:--}" --identifier com.remotemaclan.host "$app"
echo "Built $app"
