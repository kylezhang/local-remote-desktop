#!/bin/bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd -P)"
cd "$root"
work="${1:?Temporary fixture output required}"
mkdir -p "$work"
python3 tests/ReleaseChecks/check.py
python3 tests/LocalizationChecks/coverage.py
while IFS= read -r script; do bash -n "$script"; done < <(git ls-files '*.sh')
python3 tests/InstallerChecks/check.py --bash /bin/bash --work-dir "$work/installer"
python3 tests/InstallerChecks/signing.py --bash /bin/bash --work-dir "$work/signing"
python3 tests/MacPackageChecks/check.py --bash /bin/bash --work-dir "$work/package"
echo 'PASS source and packaging fixtures; native capture/permissions/signing acceptance not run'
