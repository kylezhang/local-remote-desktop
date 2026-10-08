#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
if [[ "$(uname -m)" != arm64 ]]; then echo "Apple Silicon required" >&2; exit 1; fi
identity="${SIGN_IDENTITY:-}"
identity_file="$PWD/.signing-identity"
stable_mode="${RML_REQUIRE_STABLE_SIGNATURE:-0}"
if [[ -f "$PWD/.require-stable-signature" ]]; then stable_mode=1; fi
if [[ -z "$identity" && -f "$identity_file" ]]; then identity="$(cat "$identity_file")"; fi
if [[ -z "$identity" ]]; then
  identities="$(security find-identity -v -p codesigning)"
  identity="$(printf '%s\n' "$identities" | awk '/"Developer ID Application:/ { print $2; exit }')"
  if [[ -z "$identity" ]]; then identity="$(printf '%s\n' "$identities" | awk '/"Apple Development:/ { print $2; exit }')"; fi
fi
if [[ -z "$identity" ]]; then identity="-"; fi
if [[ "$identity" == "-" && "$stable_mode" == 1 ]]; then
  echo '稳定签名模式：未找到固定的有效签名身份，拒绝退回 ad-hoc；已安装应用不会被替换。' >&2
  exit 1
fi
# Newer SwiftPM defaults to Swift Build. Use the stable native backend and
# a separate cache so products from different backends are never mixed.
xcrun swift build --build-system native --scratch-path .build-native -c release
bin_dir="$(xcrun swift build --build-system native --scratch-path .build-native -c release --show-bin-path)"
app="$PWD/build/RemoteMacLAN.app"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
cp "$bin_dir/RemoteMacLAN" "$app/Contents/MacOS/RemoteMacLAN"
cp Info.plist "$app/Contents/Info.plist"
cp ../assets/brand/LocalRemote.icns "$app/Contents/Resources/LocalRemote.icns"
codesign --force --sign "$identity" --identifier com.remotemaclan.host "$app"
codesign --verify --strict "$app"
if [[ "$identity" != "-" ]]; then
  printf '%s\n' "$identity" > "$identity_file"
  touch "$PWD/.require-stable-signature"
  echo "Reusing a fixed local signing identity for future builds."
else
  echo "开发签名（ad-hoc）：代码更新后权限和钥匙串授权可能需要重授；这不是稳定发布签名。" >&2
fi
echo "Built $app"
