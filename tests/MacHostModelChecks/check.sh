#!/bin/bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
compiler="${SWIFTC:-swiftc}"
stage="$(mktemp -d "${TMPDIR:-/tmp}/localremote-host-model-check.XXXXXX")"
trap 'rm -rf -- "$stage"' EXIT
# Compile the production model body unchanged with explicit platform service
# doubles. Imports are removed only from this disposable test copy.
sed '/^import Foundation/!{/^import /d;}' "$root/apps/macos/Sources/LocalRemoteDesktop/HostModel.swift" > "$stage/HostModel.swift"
cp "$root/apps/macos/Sources/LocalRemoteDesktop/UiText.swift" "$stage/UiText.swift"
if [[ "$(uname -s)" == Darwin ]]; then
    "$compiler" -parse-as-library "$root/apps/macos/Sources/LocalRemoteDesktop/DeviceName.swift" "$stage/UiText.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/PairingCredential.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/PairingEditor.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/SharingPreparation.swift" "$root/tests/MacHostModelChecks/Mocks.swift" "$stage/HostModel.swift" "$root/tests/MacHostModelChecks/Check.swift" -o "$stage/check"
    "$stage/check"
    if [[ "${LOCALREMOTE_SKIP_NATIVE_BUILD:-0}" != 1 ]]; then
        xcrun swift build --package-path "$root/apps/macos" --scratch-path "$stage/native-build" -c release
    fi
else
    "$compiler" -emit-library -emit-module -module-name CommonCrypto -emit-module-path "$stage/CommonCrypto.swiftmodule" "$root/tests/PairingChecks/MockCommonCrypto.swift" -lcrypto -o "$stage/libCommonCrypto.so"
    "$compiler" -parse-as-library "$root/apps/macos/Sources/LocalRemoteDesktop/DeviceName.swift" -I "$stage" -L "$stage" -lCommonCrypto -Xlinker -rpath -Xlinker "$stage" "$stage/UiText.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/PairingCredential.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/PairingEditor.swift" "$root/apps/macos/Sources/LocalRemoteDesktop/SharingPreparation.swift" "$root/tests/MacHostModelChecks/Mocks.swift" "$stage/HostModel.swift" "$root/tests/MacHostModelChecks/Check.swift" -o "$stage/check"
    "$stage/check"
fi
