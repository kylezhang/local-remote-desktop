#!/bin/bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
stage="$(mktemp -d "${TMPDIR:-/tmp}/localremote-localization-check.XXXXXX")"
trap 'rm -rf -- "$stage"' EXIT
"${SWIFTC:-swiftc}" -parse-as-library "$root/apps/macos/Sources/LocalRemote/UiText.swift" "$root/tests/LocalizationChecks/Check.swift" -o "$stage/check"
"$stage/check" "$root/assets/localization/strings.json"
