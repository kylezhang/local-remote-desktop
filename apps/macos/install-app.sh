#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
[[ "$(uname -s)" == Darwin ]] || { echo '此安装脚本只能在 macOS 运行。' >&2; exit 1; }
[[ "$EUID" != 0 ]] || { echo '请以正常登录用户运行，不要使用 sudo。' >&2; exit 1; }
# Normal updates must not revoke working permissions. A changed ad-hoc
# identity fails the preservation preflight; repair remains an explicit choice.
reset_screen=false
reset_accessibility=false
if [[ $# == 1 ]]; then
  case "$1" in
    --reset-permissions) reset_screen=true; reset_accessibility=true ;;
    --keep-permissions) reset_screen=false; reset_accessibility=false ;;
    --reset-screen-permission) reset_screen=true ;;
    *) echo '用法：bash apps/macos/install-app.sh [--keep-permissions|--reset-permissions|--reset-screen-permission]' >&2; exit 1 ;;
  esac
elif [[ $# != 0 ]]; then
  echo '用法：bash apps/macos/install-app.sh [--keep-permissions|--reset-permissions|--reset-screen-permission]' >&2; exit 1
fi

target="/Applications/Local Remote Desktop.app"
user_target="$HOME/Applications/Local Remote Desktop.app"
# Product filenames migrate; persistent bundle/Keychain/launchd identities do not.
legacy_target=/Applications/RemoteMacLAN.app
legacy_user_target="$HOME/Applications/RemoteMacLAN.app"
previous_target=/Applications/LocalRemote.app
previous_user_target="$HOME/Applications/LocalRemote.app"
old_bundles=("$target" "$user_target" "$legacy_target" "$legacy_user_target" "$previous_target" "$previous_user_target")
backup_names=(previous-system.app previous-user.app legacy-system.app legacy-user.app previous-brand-system.app previous-brand-user.app)
agent="$HOME/Library/LaunchAgents/com.remotemaclan.host.plist"
domain="gui/$(id -u)"
stage_dir=""
agent_was_loaded=false
agent_removed=false
installed=false
replacement_started=false
preserve_stage=false

cleanup() {
  local code=$?
  trap - EXIT
  # Until the new bundle passes final verification, keep the complete old
  # bundles in the private staging directory. Never merge old/new contents.
  if [[ "$replacement_started" == true && "$installed" == false ]]; then
    echo '新应用安装未完成，正在恢复旧应用包。' >&2
    local backup destination index
    for index in "${!old_bundles[@]}"; do
      backup="${backup_names[$index]}"
      destination="${old_bundles[$index]}"
      if [[ -e "$stage_dir/$backup" ]]; then
        if [[ -L "$destination" ]]; then
          preserve_stage=true
        elif [[ -e "$destination" ]] && ! rm -rf -- "$destination"; then
          preserve_stage=true
        elif ! mv "$stage_dir/$backup" "$destination"; then
          preserve_stage=true
        fi
      fi
    done
    # A failed first installation has no previous system bundle to restore.
    if [[ "$new_at_target" == true && "$had_system_bundle" == false && -e "$target" && ! -L "$target" ]]; then
      rm -rf -- "$target" || preserve_stage=true
    fi
    if [[ "$preserve_stage" == true ]]; then
      echo "自动恢复未完成；完整备份保留在：$stage_dir。请勿删除此目录。" >&2
    else
      echo '安装恢复处理完成；已经重置的权限不会回滚，应用未自动启动。' >&2
    fi
  fi
  if [[ -n "$stage_dir" && "$stage_dir" == /Applications/.localremote-install.* && "$preserve_stage" == false ]]; then rm -rf -- "$stage_dir"; fi
  if [[ "$agent_removed" == true && "$installed" == false ]]; then
    echo '安装未完成；旧自启动暂时保持停止，避免启动不完整的应用。' >&2
  fi
  exit "$code"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
new_at_target=false
had_system_bundle=false

echo '[1/6] 构建最新版；构建失败时保留已安装的应用。'
bash ./build-app.sh
app="$PWD/build/Local Remote Desktop.app"
[[ -x "$app/Contents/MacOS/LocalRemoteDesktop" ]] || { echo '构建产物不完整。' >&2; exit 1; }
codesign --verify --strict "$app"
[[ -s "$app/Contents/Resources/LocalRemoteDesktop.icns" && -s "$app/Contents/Resources/LocalRemoteDesktop.png" ]] || {
  echo '新应用缺少图标资源；保留旧应用，安装中止。' >&2; exit 1
}
[[ "$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIconFile' "$app/Contents/Info.plist")" == LocalRemoteDesktop.icns ]] || {
  echo '新应用图标声明不正确；保留旧应用，安装中止。' >&2; exit 1
}
stage_dir="$(mktemp -d /Applications/.localremote-install.XXXXXX)"
[[ "$stage_dir" == /Applications/.localremote-install.* && ! -L "$stage_dir" ]] || { echo '安装暂存路径异常。' >&2; exit 1; }
ditto "$app" "$stage_dir/Local Remote Desktop.app"
codesign --verify --strict "$stage_dir/Local Remote Desktop.app"
for icon in LocalRemoteDesktop.icns LocalRemoteDesktop.png; do
  cmp -s "$app/Contents/Resources/$icon" "$stage_dir/Local Remote Desktop.app/Contents/Resources/$icon" || {
    echo '暂存图标与构建产物不一致；保留旧应用，安装中止。' >&2; exit 1
  }
done

# Restrict every deletion to these exact, non-symlink application bundles.
for old in "${old_bundles[@]}"; do
  [[ ! -L "$old" ]] || { echo "拒绝删除符号链接：$old" >&2; exit 1; }
  if [[ -e "$old" ]]; then
    bundle_id="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$old/Contents/Info.plist")"
    [[ "$bundle_id" == com.remotemaclan.host ]] || { echo "此位置不是 LocalRemoteDesktop：$old" >&2; exit 1; }
  fi
done

# Skipping tccutil cannot preserve trust when an update changes the designated
# requirement. Check BEFORE stopping the working Host or changing any decision.
if [[ "$reset_screen" == false && "$reset_accessibility" == false ]]; then
  designated_requirement() {
    codesign -d -r- "$1" 2>&1 | sed -n 's/^designated => //p'
  }
  if ! new_requirement="$(designated_requirement "$app")"; then
    echo '无法读取新版本签名要求；保留旧应用，安装中止。' >&2; exit 1
  fi
  [[ -n "$new_requirement" ]] || { echo '无法读取新版本签名要求；保留旧应用，安装中止。' >&2; exit 1; }
  for old in "${old_bundles[@]}"; do
    [[ -e "$old" ]] || continue
    old_requirement="$(designated_requirement "$old")" || old_requirement=""
    if [[ -z "$old_requirement" || "$old_requirement" != "$new_requirement" ]]; then
      echo '--keep-permissions 无法保留本次更新的签名信任：新旧签名要求不同。旧应用和权限均未改动。' >&2
      echo '临时签名的代码更新不能保证沿用授权；长期更新需要固定签名身份。显式清理安装可用 --reset-permissions，但仍须重新授权。' >&2
      exit 1
    fi
    # Also verify the sealed bundles against each other's requirement. Equality
    # alone does not establish that either sealed bundle actually verifies.
    if ! codesign --verify --strict -R "=$old_requirement" "$app" ||
       ! codesign --verify --strict -R "=$new_requirement" "$old"; then
      echo '签名兼容性验证失败；旧应用和权限均未改动，安装中止。' >&2; exit 1
    fi
  done
fi

echo '[2/6] 停止旧 Host 和已加载的登录自启动。'
if launchctl print "$domain/com.remotemaclan.host" >/dev/null 2>&1; then
  agent_was_loaded=true
  launchctl bootout "$domain/com.remotemaclan.host"
  agent_removed=true
fi
# Match the executable name, including copies launched from build/Downloads.
# No AppleScript prompt or dependency on the original launch path.
stop_hosts() {
  local signal="$1" pid name
  for name in LocalRemoteDesktop LocalRemote RemoteMacLAN; do
    while IFS= read -r pid; do
      [[ "$pid" =~ ^[0-9]+$ ]] || continue
      kill "-$signal" "$pid" 2>/dev/null || true
    done < <(pgrep -x "$name" || true)
  done
}
hosts_running() { pgrep -x LocalRemoteDesktop >/dev/null || pgrep -x LocalRemote >/dev/null || pgrep -x RemoteMacLAN >/dev/null; }
stop_hosts TERM
for attempt in {1..30}; do
  if ! hosts_running; then break; fi
  sleep 0.1
done
if hosts_running; then stop_hosts KILL; sleep 0.2; fi
if hosts_running; then echo '旧 Host 无法停止，安装已中止。' >&2; exit 1; fi

echo '[3/6] 按所选安装模式处理本应用授权。'
# Explicit permission repair. Reset only these two services for our
# bundle ID, never all apps/services or the Keychain pairing key.
if [[ "$reset_screen" == true ]]; then
  if ! tccutil reset ScreenCapture com.remotemaclan.host; then
    echo '屏幕授权重置失败，安装已中止；没有删除旧应用。' >&2; exit 1
  fi
else
  echo '保留屏幕录制授权。'
fi
if [[ "$reset_accessibility" == true ]]; then
  if ! tccutil reset Accessibility com.remotemaclan.host; then
    echo '辅助功能授权重置失败，安装已中止；没有删除旧应用。屏幕授权可能已重置。' >&2; exit 1
  fi
else
  echo '保留辅助功能授权。'
fi

echo '[4/6] 完整移除旧应用包；验证新版本前保留临时恢复副本。'
replacement_started=true
if [[ -e "$target" ]]; then
  had_system_bundle=true
  mv "$target" "$stage_dir/previous-system.app"
fi
for index in "${!old_bundles[@]}"; do
  [[ "$index" != 0 ]] || continue
  old="${old_bundles[$index]}"
  if [[ -e "$old" ]]; then mv "$old" "$stage_dir/${backup_names[$index]}"; fi
done
for old in "${old_bundles[@]}"; do
  [[ ! -e "$old" ]] || { echo '旧应用删除失败。' >&2; exit 1; }
done

echo '[5/6] 安装并验证新应用。'
mv "$stage_dir/Local Remote Desktop.app" "$target"
new_at_target=true
codesign --verify --strict "$target"
version="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' "$target/Contents/Info.plist")"
installed=true
touch "$target"

# Existing agents keep their stable label but must no longer point at the old
# bundle/executable. Rewrite only ProgramArguments:0 after successful installation.
if [[ -f "$agent" ]]; then
  if ! /usr/libexec/PlistBuddy -c "Set :ProgramArguments:0 $target/Contents/MacOS/LocalRemoteDesktop" "$agent" ||
     ! plutil -lint "$agent"; then
    echo '新应用已安装，但自启动路径更新失败；请运行 apps/macos/install-launch-agent.sh。' >&2
    exit 1
  fi
fi

echo '[6/6] 启动唯一的新版本。'
if [[ "$agent_was_loaded" == true ]]; then
  [[ -f "$agent" ]] || { echo '新应用已安装；原登录自启动配置缺失，请重新安装 LaunchAgent。' >&2; exit 1; }
  if ! launchctl bootstrap "$domain" "$agent"; then
    echo "新应用已安装，但登录自启动失败；可手动打开 $target，之后重新配置 LaunchAgent。" >&2; exit 1
  fi
else
  if ! open "$target"; then
    echo "新应用已安装，但自动打开失败；请手动打开 $target。" >&2; exit 1
  fi
fi
echo "安装完成：Local Remote Desktop $version，位置 $target"
echo '配对密钥保留；新图标会在启动时主动设置到程序坞。'
if [[ "$reset_screen" == true || "$reset_accessibility" == true ]]; then
  echo '已按选项重置本应用授权；需要重新允许相应权限，然后退出并重开，不要再次 install。'
  echo 'tccutil 清理授权记录；系统设置列表是否立即移除旧行由 macOS 决定。'
else
  echo '新旧签名要求已核验兼容；本次跳过两项授权重置。系统仍决定授权是否有效。'
fi
