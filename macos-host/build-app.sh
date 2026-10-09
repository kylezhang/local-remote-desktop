#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
if [[ "$(uname -m)" != arm64 ]]; then echo "Apple Silicon required" >&2; exit 1; fi
identity="${SIGN_IDENTITY:-}"
release_mode="${RML_RELEASE_BUILD:-0}"
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
if [[ "$release_mode" == 1 ]]; then
  # Only a fingerprint from the current valid Developer ID list is accepted.
  # A saved identity/name is not evidence of distribution eligibility.
  identities="$(security find-identity -v -p codesigning)"
  release_identity="$(printf '%s\n' "$identities" | awk -v selected="$identity" '$2 == selected && /"Developer ID Application:/ { print $2; exit }')"
  if [[ -z "$release_identity" ]]; then
    echo 'Release build requires a valid Developer ID Application fingerprint in SIGN_IDENTITY.' >&2
    exit 1
  fi
fi
if [[ "$identity" == "-" && "$stable_mode" == 1 ]]; then
  echo '稳定签名模式：未找到固定的有效签名身份，拒绝退回 ad-hoc；已安装应用不会被替换。' >&2
  exit 1
fi
# Newer SwiftPM defaults to Swift Build. Use the stable native backend and
# a separate cache so products from different backends are never mixed.
xcrun swift build --build-system native --scratch-path .build-native -c release
bin_dir="$(xcrun swift build --build-system native --scratch-path .build-native -c release --show-bin-path)"
app="$PWD/build/RemoteMacLAN.app"
mkdir -p "$PWD/build"
[[ ! -L "$PWD/build" && ! -L "$app" ]] || { echo 'Build output must not be a symlink.' >&2; exit 1; }
stage="$(mktemp -d "$PWD/build/.bundle.XXXXXXXX")"
output_app="$app"
cleanup_bundle() {
  local status=$?
  trap - EXIT
  if [[ -d "$stage/previous.app" && ! -e "$output_app" ]]; then
    if ! mv "$stage/previous.app" "$output_app"; then
      echo "Build rollback failed; previous bundle retained at $stage/previous.app" >&2
      exit 1
    fi
  fi
  rm -rf "$stage"
  exit "$status"
}
trap cleanup_bundle EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
app="$stage/RemoteMacLAN.app"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
cp "$bin_dir/RemoteMacLAN" "$app/Contents/MacOS/RemoteMacLAN"
cp Info.plist "$app/Contents/Info.plist"
cp ../assets/brand/LocalRemote.icns "$app/Contents/Resources/LocalRemote.icns"
cp ../assets/brand/icon.png "$app/Contents/Resources/LocalRemote.png"
[[ -s "$app/Contents/Resources/LocalRemote.icns" && -s "$app/Contents/Resources/LocalRemote.png" ]] || {
  echo '图标资源缺失，构建中止。' >&2; exit 1
}
# This signed resource is a startup policy hint, not proof of certificate trust.
# Development builds must not attempt Keychain authentication at application launch.
if [[ "$identity" == "-" ]]; then signing_mode=ad-hoc; else signing_mode=certificate; fi
printf '%s\n' "$signing_mode" > "$app/Contents/Resources/signing-mode.txt"
# Packaging uses a restrictive temporary umask; the app itself must remain
# readable/executable when copied into /Applications for another normal user.
find "$app" -type d -exec chmod 755 {} +
find "$app" -type f -exec chmod 644 {} +
chmod 755 "$app/Contents/MacOS/RemoteMacLAN"
sign_options=()
if [[ "$release_mode" == 1 ]]; then sign_options=(--options runtime --timestamp); fi
codesign --force --sign "$identity" --identifier com.remotemaclan.host "${sign_options[@]}" "$app"
codesign --verify --strict "$app"
if [[ -e "$output_app" ]]; then mv "$output_app" "$stage/previous.app"; fi
if ! mv "$app" "$output_app"; then
  exit 1
fi
app="$output_app"
if [[ "$identity" != "-" && "$release_mode" != 1 ]]; then
  printf '%s\n' "$identity" > "$identity_file"
  touch "$PWD/.require-stable-signature"
  echo "Reusing a fixed local signing identity for future builds."
elif [[ "$identity" == "-" ]]; then
  echo "开发签名（ad-hoc）：代码更新后权限和钥匙串授权可能需要重授；这不是稳定发布签名。" >&2
fi
echo "Built $app"
