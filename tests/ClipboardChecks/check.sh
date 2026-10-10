#!/bin/bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
compiler="${SWIFTC:-swiftc}"
stage="$(mktemp -d "${TMPDIR:-/tmp}/localremote-clipboard-check.XXXXXX")"
trap 'rm -rf -- "$stage"' EXIT
"$compiler" -parse-as-library "$root/apps/macos/Sources/LocalRemoteDesktop/TextClipboardSync.swift" "$root/tests/ClipboardChecks/Check.swift" -o "$stage/check"
"$compiler" -parse-as-library "$root/apps/macos/Sources/LocalRemoteDesktop/SharingPreferences.swift" "$root/tests/ClipboardChecks/PreferenceCheck.swift" -o "$stage/preferences"
"$stage/preferences"
"$stage/check"
if [[ "$(uname -s)" == Darwin ]]; then
    xcrun swift build --package-path "$root/apps/macos" --scratch-path "$stage/native-build" -c release
else
    "$compiler" -emit-library -emit-module -module-name AppKit -emit-module-path "$stage/AppKit.swiftmodule" "$root/tests/ClipboardChecks/MockAppKit.swift" -o "$stage/libAppKit.so"
    "$compiler" -parse-as-library -I "$stage" -L "$stage" -lAppKit -Xlinker -rpath -Xlinker "$stage" \
        "$root/apps/macos/Sources/LocalRemoteDesktop/TextClipboardSync.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/MacClipboard.swift" "$root/tests/ClipboardChecks/AdapterCheck.swift" -o "$stage/adapter"
    "$stage/adapter"
    echo 'SKIP AppKit/Apple SDK: macOS required. Production clipboard state ran with the real Swift compiler.'
fi
