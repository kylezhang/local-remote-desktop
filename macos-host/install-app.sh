#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
[[ "$(uname -s)" == Darwin ]] || { echo '此安装脚本只能在 macOS 运行。' >&2; exit 1; }
[[ "$EUID" != 0 ]] || { echo '请以正常登录用户运行，不要使用 sudo。' >&2; exit 1; }
reset_screen=false
if [[ $# == 1 && "$1" == --reset-screen-permission ]]; then reset_screen=true
elif [[ $# != 0 ]]; then echo '用法：bash macos-host/install-app.sh [--reset-screen-permission]' >&2; exit 1; fi

target=/Applications/RemoteMacLAN.app
user_target="$HOME/Applications/RemoteMacLAN.app"
agent="$HOME/Library/LaunchAgents/com.remotemaclan.host.plist"
domain="gui/$(id -u)"
stage_dir=""
agent_was_loaded=false
agent_removed=false
installed=false

cleanup() {
  local code=$?
  if [[ -n "$stage_dir" && "$stage_dir" == /Applications/.remote-mac-lan-install.* ]]; then rm -rf -- "$stage_dir"; fi
  if [[ "$agent_removed" == true && "$installed" == false ]]; then
    echo '安装未完成；旧自启动暂时保持停止，避免启动不完整的应用。' >&2
  fi
  exit "$code"
}
trap cleanup EXIT

echo '[1/6] 构建最新版；构建失败时保留已安装的应用。'
bash ./build-app.sh
app="$PWD/build/RemoteMacLAN.app"
[[ -x "$app/Contents/MacOS/RemoteMacLAN" ]] || { echo '构建产物不完整。' >&2; exit 1; }
codesign --verify --strict "$app"
stage_dir="$(mktemp -d /Applications/.remote-mac-lan-install.XXXXXX)"
[[ "$stage_dir" == /Applications/.remote-mac-lan-install.* && ! -L "$stage_dir" ]] || { echo '安装暂存路径异常。' >&2; exit 1; }
ditto "$app" "$stage_dir/RemoteMacLAN.app"
codesign --verify --strict "$stage_dir/RemoteMacLAN.app"

# Restrict every deletion to these exact, non-symlink application bundles.
for old in "$target" "$user_target"; do
  [[ ! -L "$old" ]] || { echo "拒绝删除符号链接：$old" >&2; exit 1; }
  if [[ -e "$old" ]]; then
    bundle_id="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$old/Contents/Info.plist")"
    [[ "$bundle_id" == com.remotemaclan.host ]] || { echo "此位置不是 LocalRemote：$old" >&2; exit 1; }
  fi
done

echo '[2/6] 停止旧 Host 和已加载的登录自启动。'
if launchctl print "$domain/com.remotemaclan.host" >/dev/null 2>&1; then
  agent_was_loaded=true
  launchctl bootout "$domain/com.remotemaclan.host"
  agent_removed=true
fi
# Match the executable name, including copies launched from build/Downloads.
# No AppleScript prompt or dependency on the original launch path.
stop_hosts() {
  local signal="$1" pid
  while IFS= read -r pid; do
    [[ "$pid" =~ ^[0-9]+$ ]] || continue
    kill "-$signal" "$pid" 2>/dev/null || true
  done < <(pgrep -x RemoteMacLAN || true)
}
stop_hosts TERM
for attempt in {1..30}; do
  if ! pgrep -x RemoteMacLAN >/dev/null; then break; fi
  sleep 0.1
done
if pgrep -x RemoteMacLAN >/dev/null; then stop_hosts KILL; sleep 0.2; fi
if pgrep -x RemoteMacLAN >/dev/null; then echo '旧 Host 无法停止，安装已中止。' >&2; exit 1; fi

echo '[3/6] 处理屏幕录制授权。'
# Explicitly scoped to this application's identity; do not reset other apps or
# Accessibility, and never delete the user's Keychain pairing key.
if [[ "$reset_screen" == true ]]; then
  if ! tccutil reset ScreenCapture com.remotemaclan.host; then
    echo '屏幕授权重置失败，安装已中止；没有删除旧应用。' >&2; exit 1
  fi
else
  echo '保留屏幕录制授权；仅显式传入 --reset-screen-permission 时清除。'
fi

echo '[4/6] 完整删除旧应用包（包括其内部全部文件）。'
if [[ -e "$target" ]]; then rm -rf -- /Applications/RemoteMacLAN.app; fi
if [[ -e "$user_target" ]]; then rm -rf -- "$HOME/Applications/RemoteMacLAN.app"; fi
[[ ! -e "$target" && ! -e "$user_target" ]] || { echo '旧应用删除失败。' >&2; exit 1; }

echo '[5/6] 安装并验证新应用。'
mv "$stage_dir/RemoteMacLAN.app" "$target"
codesign --verify --strict "$target"
version="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' "$target/Contents/Info.plist")"
installed=true

echo '[6/6] 启动唯一的新版本。'
if [[ "$agent_was_loaded" == true ]]; then
  [[ -f "$agent" ]] || { echo '新应用已安装；原登录自启动配置缺失，请重新安装 LaunchAgent。' >&2; exit 1; }
  launchctl bootstrap "$domain" "$agent"
else
  open "$target"
fi
echo "安装完成：LocalRemote $version，位置 $target"
echo '配对密钥和辅助功能授权记录保留。'
if [[ "$reset_screen" == true ]]; then
  echo '屏幕录制授权已重置；重新授权后退出并重开应用，不要再次运行安装脚本。'
else
  echo '安装没有重置屏幕录制授权；如需授权，授权后仅退出重开应用。'
fi
