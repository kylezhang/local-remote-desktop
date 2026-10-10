#!/bin/bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
compiler="${SWIFTC:-swiftc}"
stage="$(mktemp -d "${TMPDIR:-/tmp}/localremote-startup-check.XXXXXX")"
trap 'rm -rf -- "$stage"' EXIT
"$compiler" -parse-as-library "$root/apps/macos/Sources/LocalRemoteDesktop/SharingPreparation.swift" "$root/tests/MacStartupChecks/Check.swift" -o "$stage/check"
"$stage/check"
if [[ "$(uname -s)" == Darwin ]]; then
    # Apple SDK compile gate. Does not sign/install/open the app or reset TCC.
    xcrun swift build --package-path "$root/apps/macos" --scratch-path "$stage/native-build" -c release
else
    echo 'SKIP Apple SDK compilation: macOS required. Shared startup logic ran with the real Swift compiler.'
fi
