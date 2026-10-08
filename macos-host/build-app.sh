#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
if [[ "$(uname -m)" != arm64 ]]; then echo "Apple Silicon required" >&2; exit 1; fi
swift build -c release
app="$PWD/build/RemoteMacLAN.app"
mkdir -p "$app/Contents/MacOS"
cp .build/release/RemoteMacLAN "$app/Contents/MacOS/RemoteMacLAN"
cp Info.plist "$app/Contents/Info.plist"
codesign --force --sign "${SIGN_IDENTITY:--}" --identifier com.remotemaclan.host "$app"
echo "Built $app"
