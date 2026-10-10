#!/bin/bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
compiler="${SWIFTC:-swiftc}"
stage="$(mktemp -d "${TMPDIR:-/tmp}/localremote-controller-wire.XXXXXX")"
trap 'rm -rf -- "$stage"' EXIT
source="$root/apps/macos/Sources/LocalRemoteDesktop"
args=()
wire="$source/Wire.swift"
if [[ "$(uname -s)" != Darwin ]]; then
    cc -shared -fPIC "$root/tests/MacControllerChecks/CryptoBridge.c" -lcrypto -o "$stage/libFixtureCrypto.so"
    "$compiler" -emit-library -emit-module -module-name CryptoKit -emit-module-path "$stage/CryptoKit.swiftmodule" "$root/tests/MacControllerChecks/CryptoFixture.swift" -L "$stage" -lFixtureCrypto -Xlinker -rpath -Xlinker "$stage" -o "$stage/libCryptoKit.so"
    "$compiler" -emit-library -emit-module -module-name CommonCrypto -emit-module-path "$stage/CommonCrypto.swiftmodule" "$root/tests/PairingChecks/MockCommonCrypto.swift" -lcrypto -o "$stage/libCommonCrypto.so"
    python3 - "$wire" "$stage/Wire.swift" <<'PY'
import pathlib, sys
text = pathlib.Path(sys.argv[1]).read_text()
text = text.replace('import Darwin','import Glibc').replace('import Security\n','').replace('Darwin.','Glibc.')
text = '\n'.join(line for line in text.splitlines() if 'SO_NOSIGPIPE' not in line and 'peer.sin_len' not in line)
text = text[:text.index('func randomBytes(')] + 'func randomBytes(_ count: Int) -> Data { Data((0..<count).map { _ in UInt8.random(in: 0...255) }) }\n'
text = text.replace('IPPROTO_TCP,','Int32(IPPROTO_TCP),').replace('SHUT_RDWR','Int32(SHUT_RDWR)')
pathlib.Path(sys.argv[2]).write_text(text)
PY
    wire="$stage/Wire.swift"
    args=(-I "$stage" -L "$stage" -lCryptoKit -lCommonCrypto -Xlinker -rpath -Xlinker "$stage")
else
    # Bash 3.2 treats an empty array expansion as unbound under set -u.
    args=(-sdk "$(xcrun --show-sdk-path)")
fi
"$compiler" -parse-as-library "${args[@]}" "$source/ControllerCore.swift" "$source/PairingCredential.swift" "$wire" "$root/tests/MacControllerChecks/ProtocolCheck.swift" -o "$stage/check"
python3 - "$source/ControllerSession.swift" "$stage/ControllerSession.swift" <<'PY'
import pathlib, sys
text = pathlib.Path(sys.argv[1]).read_text().replace('import CoreGraphics\n','')
if sys.platform != 'darwin':
    text = text.replace('import Darwin','import Glibc').replace('Darwin.','Glibc.').replace('SOCK_STREAM,','Int32(SOCK_STREAM.rawValue),')
pathlib.Path(sys.argv[2]).write_text(text)
PY
"$compiler" -parse-as-library "${args[@]}" "$source/ControllerCore.swift" "$source/PairingCredential.swift" "$wire" "$stage/ControllerSession.swift" "$root/tests/MacControllerChecks/SessionCheck.swift" -o "$stage/session"
python3 "$root/tests/MacControllerChecks/peer.py" "$stage/check" "$stage/session"
