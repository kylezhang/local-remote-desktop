#!/bin/bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
compiler="${SWIFTC:-swiftc}"
stage="$(mktemp -d "${TMPDIR:-/tmp}/localremote-pairing-check.XXXXXX")"
trap 'rm -rf -- "$stage"' EXIT
if [[ "$(uname -s)" == Darwin ]]; then
    "$compiler" -parse-as-library "$root/apps/macos/Sources/LocalRemoteDesktop/PairingCredential.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/SharingPreparation.swift" "$root/tests/PairingChecks/Check.swift" -o "$stage/check"
    "$stage/check"
    if [[ "${LOCALREMOTE_SKIP_NATIVE_BUILD:-0}" != 1 ]]; then
    if [[ "${LOCALREMOTE_SKIP_NATIVE_BUILD:-0}" != 1 ]]; then
            xcrun swift build --package-path "$root/apps/macos" --scratch-path "$stage/native-build" -c release
    fi
    fi
else
    "$compiler" -emit-library -emit-module -module-name CommonCrypto -emit-module-path "$stage/CommonCrypto.swiftmodule" "$root/tests/PairingChecks/MockCommonCrypto.swift" -lcrypto -o "$stage/libCommonCrypto.so"
    "$compiler" -parse-as-library -I "$stage" -L "$stage" -lCommonCrypto -Xlinker -rpath -Xlinker "$stage" "$root/apps/macos/Sources/LocalRemoteDesktop/PairingCredential.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/SharingPreparation.swift" "$root/tests/PairingChecks/Check.swift" -o "$stage/check"
    "$stage/check"
    echo 'SKIP native Apple SDK/Keychain/UI; Linux CommonCrypto fixture uses OpenSSL.'
fi
