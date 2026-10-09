#!/bin/bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
compiler="${SWIFTC:-swiftc}"
stage="$(mktemp -d "${TMPDIR:-/tmp}/localremote-host-model-check.XXXXXX")"
trap 'rm -rf -- "$stage"' EXIT
# Compile the production model body unchanged with explicit platform service
# doubles. Imports are removed only from this disposable test copy.
sed '/^import Foundation/!{/^import /d;}' "$root/apps/macos/Sources/LocalRemote/HostModel.swift" > "$stage/HostModel.swift"
if [[ "$(uname -s)" == Darwin ]]; then
    "$compiler" -parse-as-library "$root/apps/macos/Sources/LocalRemote/PairingCredential.swift" "$root/apps/macos/Sources/LocalRemote/PairingEditor.swift" "$root/apps/macos/Sources/LocalRemote/SharingPreparation.swift" "$root/tests/MacHostModelChecks/Mocks.swift" "$stage/HostModel.swift" "$root/tests/MacHostModelChecks/Check.swift" -o "$stage/check"
    "$stage/check"
    xcrun swift build --package-path "$root/apps/macos" --scratch-path "$stage/native-build" -c release
else
    "$compiler" -emit-library -emit-module -module-name CommonCrypto -emit-module-path "$stage/CommonCrypto.swiftmodule" "$root/tests/PairingChecks/MockCommonCrypto.swift" -lcrypto -o "$stage/libCommonCrypto.so"
    "$compiler" -parse-as-library -I "$stage" -L "$stage" -lCommonCrypto -Xlinker -rpath -Xlinker "$stage" "$root/apps/macos/Sources/LocalRemote/PairingCredential.swift" "$root/apps/macos/Sources/LocalRemote/PairingEditor.swift" "$root/apps/macos/Sources/LocalRemote/SharingPreparation.swift" "$root/tests/MacHostModelChecks/Mocks.swift" "$stage/HostModel.swift" "$root/tests/MacHostModelChecks/Check.swift" -o "$stage/check"
    "$stage/check"
fi
