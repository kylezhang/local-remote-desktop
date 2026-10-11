#!/bin/bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
compiler="${SWIFTC:-swiftc}"
stage="$(mktemp -d "${TMPDIR:-/tmp}/localremote-discovery-check.XXXXXX")"
trap 'rm -rf -- "$stage"' EXIT
"$compiler" -parse-as-library "$root/apps/macos/Sources/LocalRemoteDesktop/DeviceName.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/HostDiscovery.swift" "$root/tests/DiscoveryChecks/MockNetService.swift" -o "$stage/check"
"$stage/check"
if [[ "$(uname -s)" == Darwin ]]; then
    xcrun swift build --package-path "$root/apps/macos" --scratch-path "$stage/native-build" -c release
fi
