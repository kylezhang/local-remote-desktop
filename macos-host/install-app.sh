#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
app="$PWD/build/RemoteMacLAN.app"
[[ -d "$app" ]] || { echo "Run bash macos-host/build-app.sh first" >&2; exit 1; }
codesign --verify --strict "$app"
target=/Applications/RemoteMacLAN.app
# Verify a complete staged bundle before removing the installed one.
[[ ! -L "$target" ]] || { echo "Refusing to replace a symbolic link at $target" >&2; exit 1; }
if [[ -e "$target" ]]; then
  bundle_id="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$target/Contents/Info.plist")"
  [[ "$bundle_id" == com.remotemaclan.host ]] || { echo "Unexpected application at $target" >&2; exit 1; }
fi
stage_dir="$(mktemp -d /Applications/.remote-mac-lan-install.XXXXXX)"
[[ "$stage_dir" == /Applications/.remote-mac-lan-install.* ]] || { echo "Unexpected staging path" >&2; exit 1; }
trap 'rm -rf -- "$stage_dir"' EXIT
ditto "$app" "$stage_dir/RemoteMacLAN.app"
codesign --verify --strict "$stage_dir/RemoteMacLAN.app"
# Do not overwrite an executable while the installed application is running.
if pgrep -f '^/Applications/RemoteMacLAN.app/Contents/MacOS/RemoteMacLAN($| )' >/dev/null; then
  osascript -e 'tell application id "com.remotemaclan.host" to quit'
  for attempt in {1..30}; do
    if ! pgrep -f '^/Applications/RemoteMacLAN.app/Contents/MacOS/RemoteMacLAN($| )' >/dev/null; then break; fi
    sleep 0.1
  done
  if pgrep -f '^/Applications/RemoteMacLAN.app/Contents/MacOS/RemoteMacLAN($| )' >/dev/null; then
    echo "Host is still running; quit it before installing." >&2; exit 1
  fi
fi
# Remove only the verified fixed application path, not Keychain or TCC data.
if [[ -e "$target" ]]; then rm -rf -- /Applications/RemoteMacLAN.app; fi
mv "$stage_dir/RemoteMacLAN.app" "$target"
codesign --verify --strict "$target"
open "$target"
