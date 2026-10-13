"""Validate an explicitly ad-hoc CI preview; never grants distribution eligibility."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import re

spec = importlib.util.spec_from_file_location('package_checks', Path(__file__).with_name('check-macos-package.py'))
checks = importlib.util.module_from_spec(spec)
spec.loader.exec_module(checks)


def validate(app, version, commit):
    if not re.fullmatch(r'\d+\.\d+\.\d+(?:-(?:alpha|beta|rc)\.[1-9]\d*)?', version):
        raise ValueError('Invalid preview version')
    if not re.fullmatch(r'[0-9a-f]{40}', commit):
        raise ValueError('A full source commit is required')
    info = checks.check_app(app, version, preview=True)
    if (Path(app) / 'Contents/Resources/source-revision.txt').read_text().strip() != commit:
        raise ValueError('Preview/source commit mismatch')
    return info


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--app', required=True)
    parser.add_argument('--version', required=True)
    parser.add_argument('--commit', required=True)
    parser.add_argument('--manifest-output')
    args = parser.parse_args()
    info = validate(args.app, args.version, args.commit)
    if args.manifest_output:
        payload = Path(args.app).parent
        files = {p.relative_to(payload).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
                 for p in payload.rglob('*') if p.is_file()}
        Path(args.manifest_output).write_text(json.dumps({
            'version': args.version, 'build': info['CFBundleVersion'], 'commit': args.commit,
            'platform': 'macos-arm64', 'kind': 'development-preview',
            'signing': 'ad-hoc', 'notarized': False, 'sha256': files,
        }, indent=2) + '\n', encoding='utf-8')
    print('PASS ad-hoc preview structure, identity and source provenance; not a notarized release')


if __name__ == '__main__':
    main()
