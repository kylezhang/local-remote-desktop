#!/bin/bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
stage="$(mktemp -d "${TMPDIR:-/tmp}/lrd-name-check.XXXXXX")"
trap 'rm -rf -- "$stage"' EXIT
"${SWIFTC:-swiftc}" -parse-as-library "$root/apps/macos/Sources/LocalRemoteDesktop/DeviceName.swift" "$root/tests/DeviceNameChecks/Check.swift" -o "$stage/check"
"$stage/check"
