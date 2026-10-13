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
    -e 's/@ViewState private var category = "general"/@ViewState private var category = MacLayoutCheck.category/' \
    "$root/apps/macos/Sources/LocalRemoteDesktop/App.swift" > "$stage/App.swift"
sources=()
for source in "$root"/apps/macos/Sources/LocalRemoteDesktop/*.swift; do
    [[ "$source" == */App.swift ]] || sources+=("$source")
done
xcrun swiftc -parse-as-library -O -target "$(uname -m)-apple-macosx13.0" \
    "${sources[@]}" "$stage/App.swift" "$root/tests/MacLayoutChecks/Check.swift" -o "$app/Contents/MacOS/LayoutChecks"
cp "$root/assets/localization/strings.json" "$app/Contents/Resources/"
cp "$root/assets/interface/"{initiate-connection,accept-connection}.png "$app/Contents/Resources/"
output="${LOCALREMOTE_LAYOUT_OUTPUT:-$stage/renders}"
# Execute directly: never `open`, orderFront, activate, capture a desktop or post input.
"$app/Contents/MacOS/LayoutChecks" "$output"
if [[ -n "${LOCALREMOTE_LAYOUT_OUTPUT:-}" ]]; then
    echo "Native layout check complete. Renders: $output"
else
    echo 'Native layout check complete; temporary renders removed on exit. Set LOCALREMOTE_LAYOUT_OUTPUT to retain them.'
fi
