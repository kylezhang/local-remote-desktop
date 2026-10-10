#!/bin/bash
set -euo pipefail
[[ "$(uname -s)" == Darwin ]] || { echo 'Native VideoToolbox check requires macOS.' >&2; exit 1; }
root="$(cd "$(dirname "$0")/../.." && pwd)"
stage="$(mktemp -d "${TMPDIR:-/tmp}/localremote-native-video.XXXXXX")"
trap 'rm -rf -- "$stage"' EXIT
source="$root/apps/macos/Sources/LocalRemoteDesktop"
"${SWIFTC:-swiftc}" -parse-as-library "$source/ControllerCore.swift" "$source/ViewerDecoder.swift" \
    "$root/tests/MacControllerChecks/NativeVideoCheck.swift" -o "$stage/check"
"$stage/check"
