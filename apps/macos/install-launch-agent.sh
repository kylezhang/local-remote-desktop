#!/bin/bash
set -euo pipefail
app="/Applications/LocalRemote.app"
[[ -x "$app/Contents/MacOS/LocalRemote" ]] || { echo "Install the built app at $app first" >&2; exit 1; }
dest="$HOME/Library/LaunchAgents/com.remotemaclan.host.plist"
mkdir -p "$(dirname "$dest")"
cat > "$dest" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>Label</key><string>com.remotemaclan.host</string>
<key>ProgramArguments</key><array><string>/Applications/LocalRemote.app/Contents/MacOS/LocalRemote</string></array>
<key>RunAtLoad</key><true/>
<key>LimitLoadToSessionType</key><string>Aqua</string>
</dict></plist>
PLIST
plutil -lint "$dest"
launchctl bootout "gui/$(id -u)" "$dest" 2>/dev/null || true
launchctl bootstrap "gui/$(id -u)" "$dest"
echo "Login auto-start installed. Requires a logged-in GUI session."
