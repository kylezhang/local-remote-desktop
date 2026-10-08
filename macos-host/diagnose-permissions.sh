#!/bin/bash
set -euo pipefail
app=/Applications/RemoteMacLAN.app
echo 'Installed application signature:'
codesign --verify --strict "$app"
codesign -d -r- --verbose=2 "$app" 2>&1
echo 'Available code signing identities:'
security find-identity -v -p codesigning
# Signature details only; never reads the pairing key or Keychain secret values.
