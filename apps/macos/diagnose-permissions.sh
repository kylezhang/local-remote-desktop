#!/bin/bash
set -euo pipefail
app="/Applications/Local Remote Desktop.app"
if [[ ! -d "$app" && -d /Applications/LocalRemote.app ]]; then app=/Applications/LocalRemote.app; fi
if [[ ! -d "$app" && -d /Applications/RemoteMacLAN.app ]]; then app=/Applications/RemoteMacLAN.app; fi
echo 'Installed application and source:'
printf '%s\n' "$app"
/usr/libexec/PlistBuddy -c 'Print :CFBundleVersion' "$app/Contents/Info.plist"
if [[ -f "$app/Contents/Resources/source-revision.txt" ]]; then cat "$app/Contents/Resources/source-revision.txt"; else echo 'Source commit: unknown (older build)'; fi
shasum -a 256 "$app/Contents/MacOS/LocalRemoteDesktop"
echo 'Running product executable (diagnostics only):'
for pid in $(pgrep -x LocalRemoteDesktop || true); do ps -p "$pid" -o pid= -o comm=; done
echo 'Installed application signature:'
codesign --verify --strict "$app"
codesign -d -r- --verbose=2 "$app" 2>&1
echo 'Installed icon declaration:'
icon_file="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIconFile' "$app/Contents/Info.plist")"
echo "$icon_file"
echo 'Installed icon resources:'
for icon in "$icon_file" "${icon_file%.icns}.png"; do
  if [[ -s "$app/Contents/Resources/$icon" ]]; then shasum -a 256 "$app/Contents/Resources/$icon"
  else echo "MISSING: $icon"; fi
done
echo 'Available code signing identities:'
security find-identity -v -p codesigning
# Signature details only; never reads the pairing key or Keychain secret values.
