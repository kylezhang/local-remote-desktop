#!/bin/bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
compiler="${SWIFTC:-swiftc}"
stage="$(mktemp -d "${TMPDIR:-/tmp}/localremote-file-check.XXXXXX")"
trap 'rm -rf -- "$stage"' EXIT
"$compiler" -parse-as-library "$root/apps/macos/Sources/LocalRemoteDesktop/FileModels.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/ControllerFiles.swift" "$root/tests/FileChecks/ControllerCheck.swift" -o "$stage/controller"
"$stage/controller"
if [[ "$(uname -s)" == Darwin ]]; then
    "$compiler" -parse-as-library "$root/apps/macos/Sources/LocalRemoteDesktop/FileModels.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/FileTransport.swift" "$root/tests/FileChecks/MockWire.swift" "$root/tests/FileChecks/Check.swift" -o "$stage/check"
    "$stage/check"
    xcrun swift build --package-path "$root/apps/macos" --scratch-path "$stage/native-build" -c release
else
    "$compiler" -emit-library -emit-module -module-name CryptoKit -emit-module-path "$stage/CryptoKit.swiftmodule" "$root/tests/FileChecks/MockCryptoKit.swift" -o "$stage/libCryptoKit.so"
    "$compiler" -parse-as-library -I "$stage" -L "$stage" -lCryptoKit -Xlinker -rpath -Xlinker "$stage" \
        "$root/apps/macos/Sources/LocalRemoteDesktop/FileModels.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/FileTransport.swift" "$root/tests/FileChecks/MockWire.swift" "$root/tests/FileChecks/Check.swift" -o "$stage/check"
    "$stage/check"
    "$compiler" -emit-library -emit-module -module-name AppKit -emit-module-path "$stage/AppKit.swiftmodule" "$root/tests/ClipboardChecks/MockAppKit.swift" -o "$stage/libAppKit.so"
    "$compiler" -emit-library -emit-module -module-name Darwin -emit-module-path "$stage/Darwin.swiftmodule" "$root/tests/FileChecks/MockDarwin.swift" -o "$stage/libDarwin.so"
    "$compiler" -parse-as-library -I "$stage" -L "$stage" -lCryptoKit -lAppKit -lDarwin -Xlinker -rpath -Xlinker "$stage" \
        "$root/apps/macos/Sources/LocalRemoteDesktop/FileModels.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/FileTransport.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/MacFiles.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/ControllerFiles.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/MacControllerFiles.swift" "$root/tests/FileChecks/MockWire.swift" "$root/tests/FileChecks/AdapterCheck.swift" -o "$stage/adapter"
    "$stage/adapter"
    echo 'SKIP Apple SDK/native Mac clipboard/socket integration. Linux fixture uses OpenSSL for test hashing.'
fi
