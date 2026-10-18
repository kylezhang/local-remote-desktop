#!/bin/bash
set -euo pipefail
[[ "$(uname -s)" == Darwin ]] || { echo 'Native Mac layout checks require macOS.' >&2; exit 1; }
root="$(cd "$(dirname "$0")/../.." && pwd -P)"
stage="$(mktemp -d "${TMPDIR:-/tmp}/localremote-native-layout.XXXXXXXX")"
trap 'rm -rf -- "$stage"' EXIT
app="$stage/LayoutChecks.app"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
cat > "$app/Contents/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleIdentifier</key><string>org.localremotedesktop.layoutchecks</string>
<key>CFBundleExecutable</key><string>LayoutChecks</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>LSUIElement</key><true/>
</dict></plist>
PLIST
# The application entry point is excluded, not replaced with its real delegate.
# Only displayed local addresses use documentation fixtures; no actual device
# details may appear in the synthetic native renders.
sed -e '/^@main$/d' -e 's/ForEach(LocalInterfaces.addresses,id:/ForEach(["192.0.2.10"],id:/' \
    "$root/apps/macos/Sources/LocalRemoteDesktop/App.swift" > "$stage/App.swift"
# Seed the long-error layout only in this disposable source copy. A same-file
# extension can access the private setter without exposing it in the app.
cp "$root/apps/macos/Sources/LocalRemoteDesktop/ControllerView.swift" "$stage/ControllerView.swift"
cat >> "$stage/ControllerView.swift" <<'SWIFT'

extension ControllerModel {
    func setLayoutFixtureNotice(_ value: String) { pairingNotice = value }
    func setLayoutFixtureSession(connected: Bool, input: Bool) {
        busy = true; self.connected = connected; inputEnabled = input
        targetName = "A long authenticated desktop name for session layout"
        status = connected ? (input ? "已连接，点击画面开始操作。" : "已连接，只能查看。") : "连接中断，正在重连…"
    }
}
SWIFT
sources=()
for source in "$root"/apps/macos/Sources/LocalRemoteDesktop/*.swift; do
    [[ "$source" == */App.swift || "$source" == */ControllerView.swift ]] || sources+=("$source")
done
xcrun swiftc -parse-as-library -O -target "$(uname -m)-apple-macosx13.0" \
    "${sources[@]}" "$stage/App.swift" "$stage/ControllerView.swift" "$root/tests/MacLayoutChecks/Check.swift" -o "$app/Contents/MacOS/LayoutChecks"
cp "$root/assets/localization/strings.json" "$app/Contents/Resources/"
cp "$root/assets/interface/connection-stage.png" "$app/Contents/Resources/"
cp "$root/assets/interface/device-display.png" "$app/Contents/Resources/"
output="${LOCALREMOTE_LAYOUT_OUTPUT:-$stage/renders}"
# Execute directly: never `open`, orderFront, activate, capture a desktop or post input.
"$app/Contents/MacOS/LayoutChecks" "$output"
if [[ -n "${LOCALREMOTE_LAYOUT_OUTPUT:-}" ]]; then
    echo "Native layout check complete. Renders: $output"
else
    echo 'Native layout check complete; temporary renders removed on exit. Set LOCALREMOTE_LAYOUT_OUTPUT to retain them.'
fi
