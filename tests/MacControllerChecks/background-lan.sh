#!/bin/bash
set -euo pipefail
[[ "$(uname -s)" == Darwin && $# == 2 ]] || { echo 'Usage on Mac: background-lan.sh FIXTURE_PRIVATE_IP PORT' >&2; exit 1; }
root="$(cd "$(dirname "$0")/../.." && pwd -P)"
stage="$(mktemp -d "${TMPDIR:-/tmp}/lrd-background-lan.XXXXXXXX")"
trap 'rm -rf -- "$stage"' EXIT
src="$root/apps/macos/Sources/LocalRemoteDesktop"
xcrun swiftc -parse-as-library "$src/ControllerCore.swift" "$src/PairingCredential.swift" "$src/Wire.swift" \
    "$src/ViewerDecoder.swift" "$src/ControllerSession.swift" "$src/FileModels.swift" "$src/FileTransport.swift" \
    "$src/TextClipboardSync.swift" "$root/tests/MacControllerChecks/BackgroundLANCheck.swift" -o "$stage/check"
"$stage/check" "$1" "$2"
