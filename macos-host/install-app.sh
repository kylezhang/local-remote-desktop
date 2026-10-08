#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
app="$PWD/build/RemoteMacLAN.app"
[[ -d "$app" ]] || { echo "Run bash macos-host/build-app.sh first" >&2; exit 1; }
codesign --verify --strict "$app"
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
ditto "$app" /Applications/RemoteMacLAN.app
codesign --verify --strict /Applications/RemoteMacLAN.app
open /Applications/RemoteMacLAN.app
