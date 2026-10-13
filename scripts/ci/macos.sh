#!/bin/bash
set -euo pipefail
[[ "$(uname -s)" == Darwin && "$(uname -m)" == arm64 ]] || { echo 'Apple Silicon runner required' >&2; exit 1; }
root="$(cd "$(dirname "$0")/../.." && pwd -P)"
cd "$root"
output="${1:?Artifact output required}"
mkdir -p "$output/reports"
xcrun swift --version > "$output/reports/swift.txt"
xcodebuild -version > "$output/reports/xcode.txt"
# Compile the full app once in package-macos-preview.sh, not after each fixture.
export LOCALREMOTE_SKIP_NATIVE_BUILD=1
for suite in PairingChecks MacStartupChecks MacHostModelChecks MacControllerChecks DeviceNameChecks DiscoveryChecks ClipboardChecks FileChecks LocalizationChecks; do
    bash "tests/$suite/check.sh" 2>&1 | tee "$output/reports/$suite.txt"
done
LOCALREMOTE_LAYOUT_OUTPUT="$output/reports/layouts" bash tests/MacLayoutChecks/check.sh 2>&1 | tee "$output/reports/MacLayoutChecks.txt"
echo 'PASS native Swift fixtures/layouts; TCC, real capture/input and notarization not run'
