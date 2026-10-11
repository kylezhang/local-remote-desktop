#!/bin/bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
compiler="${SWIFTC:-swiftc}"
stage="$(mktemp -d "${TMPDIR:-/tmp}/localremote-controller-check.XXXXXX")"
trap 'rm -rf -- "$stage"' EXIT
"$compiler" -parse-as-library "$root/apps/macos/Sources/LocalRemoteDesktop/DeviceName.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/ControllerCore.swift" "$root/tests/MacControllerChecks/Check.swift" -o "$stage/check"
"$stage/check"
{ echo 'import Foundation'; sed '/^struct ControllerView:/,$d' "$root/apps/macos/Sources/LocalRemoteDesktop/ControllerView.swift" | sed '/^import /d'; } > "$stage/ControllerModel.swift"
"$compiler" -parse-as-library "$root/apps/macos/Sources/LocalRemoteDesktop/DeviceName.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/ControllerCore.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/ControllerMemory.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/TextClipboardSync.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/ControllerTextClipboard.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/FileModels.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/ControllerFiles.swift" "$root/tests/MacControllerChecks/ModelMocks.swift" "$stage/ControllerModel.swift" "$root/tests/MacControllerChecks/ModelCheck.swift" -o "$stage/model"
"$stage/model"
if [[ "$(uname -s)" == Darwin ]]; then
    xcrun swift build --package-path "$root/apps/macos" --scratch-path "$stage/native-build" -c release
else
    echo 'SKIP native AppKit/SwiftUI/VideoToolbox; Foundation controller checks only.'
fi
