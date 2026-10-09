#!/bin/bash
set -euo pipefail
app=/Applications/LocalRemote.app
if [[ ! -d "$app" && -d /Applications/RemoteMacLAN.app ]]; then app=/Applications/RemoteMacLAN.app; fi
echo 'Installed application signature:'
codesign --verify --strict "$app"
codesign -d -r- --verbose=2 "$app" 2>&1
echo 'Installed icon declaration:'
/usr/libexec/PlistBuddy -c 'Print :CFBundleIconFile' "$app/Contents/Info.plist"
echo 'Installed icon resources:'
for icon in LocalRemote.icns LocalRemote.png; do
  if [[ -s "$app/Contents/Resources/$icon" ]]; then shasum -a 256 "$app/Contents/Resources/$icon"
  else echo "MISSING: $icon"; fi
done
echo 'Available code signing identities:'
security find-identity -v -p codesigning
# Signature details only; never reads the pairing key or Keychain secret values.
