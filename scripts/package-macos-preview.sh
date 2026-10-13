#!/bin/bash
# Explicit CI development package. Never installs/opens an app or changes TCC/Keychain.
set -euo pipefail
umask 077
root="$(cd "$(dirname "$0")/.." && pwd -P)"
cd "$root"
fail() { echo "$*" >&2; exit 1; }
[[ "$(uname -s)" == Darwin && "$(uname -m)" == arm64 ]] || fail 'Apple Silicon macOS required.'
[[ $# -le 1 ]] || fail 'Usage: package-macos-preview.sh [OUTPUT_DIRECTORY]'
[[ -z "$(git status --porcelain --untracked-files=normal)" ]] || fail 'Commit final source before packaging.'
commit="$(git rev-parse HEAD)"
version="$(python3 -c 'import plistlib; i=plistlib.load(open("apps/macos/Info.plist","rb")); print(i.get("LocalRemoteDesktopReleaseVersion", i["CFBundleShortVersionString"]))')"
[[ "$commit" =~ ^[0-9a-f]{40}$ ]] || fail 'Full source commit required.'
[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-(alpha|beta|rc)\.[1-9][0-9]*)?$ ]] || fail 'Invalid application version.'
output="${1:-$root/dist}"
mkdir -p "$output"
[[ ! -L "$output" ]] || fail 'Output must not be a symlink.'
output="$(cd "$output" && pwd -P)"
name="LocalRemoteDesktop-v$version-macos-arm64-preview"
archive="$output/$name.zip"
[[ ! -e "$archive" ]] || fail 'Output already exists; refusing overwrite.'
stage="$(mktemp -d "$output/.macos-preview.XXXXXXXX")"
trap 'rm -rf -- "$stage"' EXIT
payload="$stage/$name"
mkdir -p "$payload"
SIGN_IDENTITY=- LOCALREMOTE_RELEASE_BUILD=0 bash apps/macos/build-app.sh
app="$payload/Local Remote Desktop.app"
ditto "apps/macos/build/Local Remote Desktop.app" "$app"
codesign --verify --strict "$app"
cp LICENSE NOTICE.md "$payload/"
git archive --format=zip --prefix="LocalRemoteDesktop-source-$commit/" --output="$payload/source.zip" "$commit"
cat > "$payload/SOURCE.txt" <<EOF
Local Remote Desktop $version — development preview
Source commit: $commit
Corresponding source: source.zip; license: GPL-3.0-or-later (LICENSE).
Extract source.zip and follow README.md / apps/macos/build-app.sh.
Third-party notices: NOTICE.md. Built on Apple Silicon using the Apple SDK.
Signing: ad-hoc. Not notarized; this is not a signed product release.
EOF
cat > "$payload/START-HERE.txt" <<EOF
Local Remote Desktop $version — Apple Silicon development preview

Quit an older copy, then copy the complete Local Remote Desktop.app to /Applications.
Open the app; choose Initiate connection or Accept connections.
Allow Screen Recording and Accessibility when macOS asks for desktop sharing.
macOS may block this downloaded ad-hoc preview. Review the app in Privacy & Security;
if macOS still rejects it, use a local source build rather than disabling Gatekeeper.
Changed ad-hoc code can require renewed permissions/Keychain approval.
This package is not Developer ID signed or notarized. CI checks do not establish
real remote-control, permission, clipboard or sustained-performance acceptance.
Normal logged-in sessions only; no FileVault preboot or login-screen guarantee.

退出旧应用，将完整 Local Remote Desktop.app 复制到 /Applications 后打开。
选择“发起连接”或“接受连接”，共享时按系统提示授予屏幕录制和辅助功能权限。
这是临时签名的开发预览包，未公证；系统可能阻止打开，更新后也可能需要重新授权。
请在“隐私与安全性”查看系统提示；不能打开时可从源码构建，不要关闭 Gatekeeper。
后台 CI 检查不等于真实远控、权限、剪贴板和长时间性能验收。

Source commit: $commit
Repository: https://atomgit.com/zkxw2008/local-remote-desktop
EOF
python3 scripts/check-macos-preview.py --app "$app" --version "$version" --commit "$commit" --manifest-output "$payload/preview.json"
[[ "$(git rev-parse HEAD)" == "$commit" && -z "$(git status --porcelain --untracked-files=normal)" ]] || fail 'Source changed during preview build.'
ditto -c -k --keepParent "$payload" "$stage/preview.zip"
mkdir "$stage/extracted"
ditto -x -k "$stage/preview.zip" "$stage/extracted"
extracted="$stage/extracted/$name/Local Remote Desktop.app"
codesign --verify --strict "$extracted"
python3 scripts/check-macos-preview.py --app "$extracted" --version "$version" --commit "$commit"
python3 - "$stage/extracted/$name/source.zip" "$commit" "$payload/LICENSE" <<'PY'
import importlib.util, pathlib, sys
spec=importlib.util.spec_from_file_location('checks','scripts/check-macos-package.py')
checks=importlib.util.module_from_spec(spec); spec.loader.exec_module(checks)
checks.check_source_archive(pathlib.Path(sys.argv[1]).read_bytes(),sys.argv[2],pathlib.Path(sys.argv[3]).read_bytes())
PY
mv "$stage/preview.zip" "$archive"
(cd "$output" && shasum -a 256 "$name.zip" > SHA256SUMS.txt)
python3 - "$output/ci.json" "$version" "$commit" <<'PY'
import json, pathlib, subprocess, sys
pathlib.Path(sys.argv[1]).write_text(json.dumps({
 'version':sys.argv[2], 'commit':sys.argv[3], 'platform':'macos-arm64',
 'kind':'development-preview','signing':'ad-hoc','notarized':False,
 'swift':subprocess.check_output(['xcrun','swift','--version'],text=True).strip(),
 'notRun':['Real capture/input','TCC/Keychain acceptance','Notarization','Sustained performance']
},indent=2)+'\n',encoding='utf-8')
PY
chmod 644 "$archive" "$output/SHA256SUMS.txt" "$output/ci.json"
if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
    printf 'macOS arm64: native build, fixtures, layout and extracted preview signature checks passed. Source: %s. Ad-hoc / not notarized; physical acceptance not run.\n' "$commit" >> "$GITHUB_STEP_SUMMARY"
fi
echo "Packaged development preview: $archive"
