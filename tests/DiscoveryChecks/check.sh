#!/bin/bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
compiler="${SWIFTC:-swiftc}"
stage="$(mktemp -d "${TMPDIR:-/tmp}/localremote-discovery-check.XXXXXX")"
trap 'rm -rf -- "$stage"' EXIT
"$compiler" -parse-as-library "$root/apps/macos/Sources/LocalRemoteDesktop/DeviceName.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/HostDiscovery.swift" "$root/tests/DiscoveryChecks/MockNetService.swift" -o "$stage/check"
"$stage/check"
"$compiler" -parse-as-library "$root/apps/macos/Sources/LocalRemoteDesktop/DeviceName.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/ControllerCore.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/ControllerMemory.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/ControllerDiscovery.swift" "$root/tests/DiscoveryChecks/MockBrowser.swift" -o "$stage/browser"
"$stage/browser"
if [[ "$(uname -s)" == Darwin ]]; then
    if [[ "${LOCALREMOTE_SKIP_NATIVE_BUILD:-0}" != 1 ]]; then
        xcrun swift build --package-path "$root/apps/macos" --scratch-path "$stage/native-build" -c release
    fi
fi
