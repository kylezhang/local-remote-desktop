#!/bin/bash
# Maintainer-only pipeline. Never installs, launches, resets TCC or reads pairing keys.
set -euo pipefail
umask 077
root="$(cd "$(dirname "$0")/.." && pwd -P)"
cd "$root"
fail() { echo "$*" >&2; exit 1; }
[[ $# -ge 1 && $# -le 2 ]] || fail 'Usage: package-macos.sh VERSION [OUTPUT_DIRECTORY]'
version="$1"
[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || fail 'Expected a semantic release version.'
[[ "$(uname -s)" == Darwin && "$(uname -m)" == arm64 ]] || fail 'An Apple Silicon Mac is required.'
[[ -n "${SIGN_IDENTITY:-}" && -n "${NOTARY_PROFILE:-}" ]] || fail 'Set SIGN_IDENTITY to a Developer ID fingerprint and NOTARY_PROFILE to an existing Keychain profile.'
command -v python3 >/dev/null || fail 'Python 3 is required for portable package checks.'
[[ -z "$(git status --porcelain --untracked-files=normal)" ]] || fail 'Commit the final source and documentation before packaging.'
commit="$(git rev-parse HEAD)"
[[ "$commit" =~ ^[0-9a-f]{40}$ ]] || fail 'Cannot identify the source commit.'
python3 tests/ReleaseChecks/check.py
actual_version="$(python3 -c 'import plistlib; print(plistlib.load(open("apps/macos/Info.plist", "rb"))["CFBundleShortVersionString"])')"
[[ "$version" == "$actual_version" ]] || fail 'Requested version does not match the committed application version.'
release_version="$(python3 -c 'import plistlib; i=plistlib.load(open("apps/macos/Info.plist", "rb")); print(i.get("LocalRemoteDesktopReleaseVersion", i["CFBundleShortVersionString"]))')"
[[ "$version" == "$release_version" ]] || fail 'Do not package a prerelease source as a stable Mac binary.'
identities="$(security find-identity -v -p codesigning)"
[[ -n "$(printf '%s\n' "$identities" | awk -v selected="$SIGN_IDENTITY" '$2 == selected && /"Developer ID Application:/ { print $2; exit }')" ]] || fail 'The selected fingerprint is not a valid Developer ID Application identity.'

output="${2:-$root/dist}"
mkdir -p "$output"
[[ ! -L "$output" ]] || fail 'Output directory must not be a symlink.'
output="$(cd "$output" && pwd -P)"
name="LocalRemoteDesktop-v$version-macos-arm64"
final="$output/$name"
[[ ! -e "$final" && ! -L "$final" ]] || fail 'Release output already exists; never overwrite release artifacts.'
stage="$(mktemp -d "$output/.macos-release.XXXXXXXX")"
trap 'rm -rf "$stage"' EXIT
payload="$stage/$name"
mkdir -p "$payload" "$stage/publish"

echo '[1/5] Compile the full Host and run startup checks; build with distribution signing.'
bash tests/MacStartupChecks/check.sh
LOCALREMOTE_RELEASE_BUILD=1 bash apps/macos/build-app.sh
ditto "apps/macos/build/Local Remote Desktop.app" "$payload/Local Remote Desktop.app"
app="$payload/Local Remote Desktop.app"
check_native_signature() {
  codesign --verify --strict -R 'anchor apple generic and certificate leaf[field.1.2.840.113635.100.6.1.13] exists' "$1"
  local details
  details="$(codesign -d --verbose=4 "$1" 2>&1)"
  printf '%s\n' "$details" | grep -q 'flags=.*(runtime)' || fail 'Hardened Runtime is missing.'
  printf '%s\n' "$details" | grep -q '^Timestamp=' || fail 'Secure signing timestamp is missing.'
  codesign -d --entitlements :- "$1" > "$stage/entitlements.plist" 2> "$stage/signature-details.txt"
  if grep -q 'com.apple.security.get-task-allow' "$stage/entitlements.plist"; then
    fail 'A debug entitlement is present in the distribution bundle.'
  fi
}
check_native_signature "$app"
python3 scripts/check-macos-package.py --app "$app" --version "$version"

echo '[2/5] Submit the signed bundle for Apple notarization using the existing profile.'
ditto -c -k --keepParent "$app" "$stage/submission.zip"
if ! xcrun notarytool submit "$stage/submission.zip" --keychain-profile "$NOTARY_PROFILE" --wait --output-format json > "$stage/notary.json" 2> "$stage/notary-errors.txt"; then
  fail 'Notarization submission failed; no release package was created. Check the maintainer profile and Apple service.'
fi
python3 -c 'import json,sys; result=json.load(open(sys.argv[1])); sys.exit(0 if result.get("status")=="Accepted" else "Notarization was not Accepted; no release package will be created.")' "$stage/notary.json"

echo '[3/5] Staple and validate the ticket; assess the application.'
xcrun stapler staple "$app"
xcrun stapler validate "$app"
check_native_signature "$app"
spctl --assess --type execute "$app"
cp NOTICE.md "$payload/NOTICE.md"
cp LICENSE "$payload/LICENSE"
git archive --format=zip --prefix="LocalRemoteDesktop-source-$commit/" --output="$payload/source.zip" "$commit"
cat > "$payload/SOURCE.txt" <<EOF
Local Remote Desktop — corresponding source
License: GNU GPL version 3 only (GPL-3.0-only); see LICENSE.
Source commit: $commit
Source archive: source.zip (included here; repository access is not required)
Extract source.zip and follow README.md and apps/macos/build-app.sh on macOS.
For distribution signing, see docs/macos-signing.md. Third-party licenses: NOTICE.md.

相应源码已附在 source.zip，提交为 $commit；解压后按 README.zh-CN.md
和 apps/macos/build-app.sh 构建。分发签名见 docs/zh-CN/macos-signing.md。
EOF
cat > "$payload/START-HERE.txt" <<EOF
Local Remote Desktop $version — macOS Apple Silicon

Quit any older Host. Copy Local Remote Desktop.app to /Applications, replacing the old bundle.
Open the installed app, allow Screen Recording and Accessibility when requested,
then quit and reopen if macOS asks. Start sharing; enter the Mac LAN IP and pairing
key in Windows. Share the pairing key only with your own trusted controller.
Requires macOS 13 or later and a normally logged-in user session.
FileVault preboot and login-before-user-session control are not supported.
A development-to-distribution signing change can require new permission approval.

退出旧 Host，将 Local Remote Desktop.app 完整替换到 /Applications，打开后按系统提示
授予屏幕录制和辅助功能权限；需要时退出重开。开始共享后，在 Windows
填写 Mac 局域网 IP 和配对密钥。仅支持正常登录后的用户会话。
从开发签名切换到发布签名时，可能需要重新授权。

Repository: https://atomgit.com/zkxw2008/local-remote-desktop
License: GPL-3.0-only; see LICENSE. Corresponding source: source.zip and SOURCE.txt.
Source commit: $commit
EOF
python3 scripts/check-macos-package.py --app "$app" --version "$version" --commit "$commit" --manifest-output "$payload/release.json"

echo '[4/5] Create the final ZIP after stapling and check the extracted artifact.'
archive="$stage/publish/$name.zip"
ditto -c -k --keepParent "$payload" "$archive"
python3 scripts/check-macos-package.py --archive "$archive" --version "$version" --commit "$commit"
mkdir "$stage/extracted"
ditto -x -k "$archive" "$stage/extracted"
check_native_signature "$stage/extracted/$name/Local Remote Desktop.app"
xcrun stapler validate "$stage/extracted/$name/Local Remote Desktop.app"
spctl --assess --type execute "$stage/extracted/$name/Local Remote Desktop.app"
[[ "$(git rev-parse HEAD)" == "$commit" && -z "$(git status --porcelain --untracked-files=normal)" ]] || fail 'Source changed during packaging; refusing to publish the candidate.'
(cd "$stage/publish" && python3 -c 'import hashlib,sys; print(hashlib.sha256(open(sys.argv[1], "rb").read()).hexdigest()+"  "+sys.argv[1])' "$name.zip" > SHA256SUMS.txt)
cp "$payload/release.json" "$stage/publish/release.json"
chmod 755 "$stage/publish"
chmod 644 "$stage/publish/"*
# mkdir is an exclusive reservation, so another build cannot be merged/overwritten.
mkdir "$final" || fail 'Another release build reserved this output; no overwrite performed.'
if ! mv "$stage/publish/"* "$final/"; then
  echo "Output transfer failed; incomplete directory retained at $final. Do not publish it." >&2
  exit 1
fi
echo "[5/5] Candidate packaged: $final"
echo 'Native first-launch and two-update acceptance are still required before AtomGit publication.'
