"""Portable package validation and mocked Mac release flow. No Apple service/keys/TCC."""
import argparse
import hashlib
import io
import importlib.util
import json
from pathlib import Path
import plistlib
import struct
import subprocess
import sys
import tempfile
import unittest
import zipfile

root = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('--bash', required=True)
parser.add_argument('--work-dir', required=True)
args, rest = parser.parse_known_args()
work = Path(args.work_dir).resolve()
work.mkdir(parents=True, exist_ok=True)
spec = importlib.util.spec_from_file_location('package_check', root/'scripts/check-macos-package.py')
checker = importlib.util.module_from_spec(spec)
spec.loader.exec_module(checker)
commit = 'a' * 40
name = 'LocalRemoteDesktop-v0.1.1-macos-arm64'


def source_archive(change='none'):
    content = io.BytesIO()
    prefix = f'LocalRemoteDesktop-source-{commit}/'
    with zipfile.ZipFile(content, 'w') as source:
        source.comment = (('b' * 40) if change == 'wrong-source-commit' else commit).encode('ascii')
        for relative in ('LICENSE', 'NOTICE.md', 'README.md', 'README.zh-CN.md',
                         'apps/macos/Package.swift', 'apps/macos/build-app.sh',
                         'apps/windows/LocalRemoteDesktop.csproj', 'apps/windows/BUILD.md'):
            if change == 'missing-build-script' and relative == 'apps/macos/build-app.sh': continue
            data = (root/relative).read_bytes()
            if change == 'wrong-source-license' and relative == 'LICENSE': data = b'wrong license'
            source.writestr(prefix + relative, data)
        if change == 'source-traversal': source.writestr(prefix + '../escape', b'fixture')
    return content.getvalue()


def bundle(app):
    (app/'Contents/MacOS').mkdir(parents=True)
    (app/'Contents/Resources').mkdir()
    (app/'Contents/_CodeSignature').mkdir()
    info = plistlib.loads((root/'apps/macos/Info.plist').read_bytes())
    info['CFBundleShortVersionString'] = '0.1.1'
    info['LocalRemoteDesktopReleaseVersion'] = '0.1.1'
    (app/'Contents/Info.plist').write_bytes(plistlib.dumps(info))
    executable = app/'Contents/MacOS/LocalRemoteDesktop'
    executable.write_bytes(struct.pack('<II', 0xFEEDFACF, 0x0100000C) + bytes(24))
    executable.chmod(0o755)
    for source, target in [('LocalRemoteDesktop.icns', 'LocalRemoteDesktop.icns'), ('icon.png', 'LocalRemoteDesktop.png')]:
        (app/'Contents/Resources'/target).write_bytes((root/'assets/brand'/source).read_bytes())
    (app/'Contents/Resources/signing-mode.txt').write_text('certificate\n')
    (app/'Contents/Resources/strings.json').write_bytes((root/'assets/localization/strings.json').read_bytes())
    for name in ('connection-stage.png','device-display.png'):
        (app/'Contents/Resources'/name).write_bytes((root/'assets/interface'/name).read_bytes())
    (app/'Contents/Resources/source-revision.txt').write_text(commit+'\n')
    (app/'Contents/_CodeSignature/CodeResources').write_text('mock signature, not cryptographic verification')


class PackageChecks(unittest.TestCase):
    def test_build_31_requires_only_its_original_shared_stage(self):
        app = self.fixture()/'Local Remote Desktop.app'
        bundle(app)
        info = plistlib.loads((app/'Contents/Info.plist').read_bytes())
        info['CFBundleVersion'] = '31'
        (app/'Contents/Info.plist').write_bytes(plistlib.dumps(info))
        (app/'Contents/Resources/device-display.png').unlink()
        checker.check_app(app, '0.1.1')
    def test_previous_home_resources_remain_valid_for_older_builds(self):
        app = self.fixture()/'Local Remote Desktop.app'
        bundle(app)
        info = plistlib.loads((app/'Contents/Info.plist').read_bytes())
        info['CFBundleVersion'] = '30'
        (app/'Contents/Info.plist').write_bytes(plistlib.dumps(info))
        (app/'Contents/Resources/connection-stage.png').unlink()
        (app/'Contents/Resources/device-display.png').unlink()
        for name in ('initiate-connection.png','accept-connection.png'):
            (app/'Contents/Resources'/name).write_bytes((root/'assets/interface'/name).read_bytes())
        checker.check_app(app, '0.1.1')

    def test_explicit_preview_is_not_a_release(self):
        app = self.fixture()/'Local Remote Desktop.app'
        bundle(app)
        info = plistlib.loads((app/'Contents/Info.plist').read_bytes())
        info['LocalRemoteDesktopReleaseVersion'] = '0.1.1-alpha.1'
        (app/'Contents/Info.plist').write_bytes(plistlib.dumps(info))
        (app/'Contents/Resources/signing-mode.txt').write_text('ad-hoc\n')
        checker.check_app(app, '0.1.1-alpha.1', preview=True)
        for version in ('0.1.1', '0.1.1-alpha.1'):
            with self.assertRaises(ValueError): checker.check_app(app, version)
        preview_spec = importlib.util.spec_from_file_location('preview_check', root/'scripts/check-macos-preview.py')
        preview = importlib.util.module_from_spec(preview_spec)
        preview_spec.loader.exec_module(preview)
        preview.validate(app, '0.1.1-alpha.1', commit)
        with self.assertRaises(ValueError): preview.validate(app, '0.1.1-alpha.1', 'b'*40)
        with self.assertRaises(ValueError): preview.validate(app, '0.1.1', commit)
        (app/'Contents/Resources/signing-mode.txt').write_text('certificate\n')
        with self.assertRaises(ValueError): preview.validate(app, '0.1.1-alpha.1', commit)

    def fixture(self):
        temporary = tempfile.TemporaryDirectory(prefix='mac-package-', dir=work)
        self.addCleanup(temporary.cleanup)
        return Path(temporary.name).resolve()

    def test_bundle_validation(self):
        for change in ('none', 'missing-icon', 'missing-language', 'bad-language', 'wrong-version', 'prerelease-as-stable', 'wrong-cpu', 'adhoc', 'extra-key', 'bad-icns', 'missing-entry', 'missing-device', 'bad-device', 'dirty-source'):
            with self.subTest(change=change):
                app = self.fixture()/'Local Remote Desktop.app'
                bundle(app)
                if change == 'missing-icon': (app/'Contents/Resources/LocalRemoteDesktop.png').unlink()
                if change == 'missing-language': (app/'Contents/Resources/strings.json').unlink()
                if change == 'bad-language': (app/'Contents/Resources/strings.json').write_text('{}')
                if change == 'wrong-version':
                    info = plistlib.loads((app/'Contents/Info.plist').read_bytes())
                    info['CFBundleShortVersionString'] = '9.9.9'
                    (app/'Contents/Info.plist').write_bytes(plistlib.dumps(info))
                if change == 'wrong-cpu': (app/'Contents/MacOS/LocalRemoteDesktop').write_bytes(bytes(32))
                if change == 'prerelease-as-stable':
                    info = plistlib.loads((app/'Contents/Info.plist').read_bytes())
                    info['LocalRemoteDesktopReleaseVersion'] = '0.1.1-beta.1'
                    (app/'Contents/Info.plist').write_bytes(plistlib.dumps(info))
                if change == 'adhoc': (app/'Contents/Resources/signing-mode.txt').write_text('ad-hoc')
                if change == 'extra-key': (app/'Contents/Resources/secret.key').write_text('fixture')
                if change == 'bad-icns': (app/'Contents/Resources/LocalRemoteDesktop.icns').write_bytes(b'icns0000')
                if change == 'missing-entry': (app/'Contents/Resources/connection-stage.png').unlink()
                if change == 'missing-device': (app/'Contents/Resources/device-display.png').unlink()
                if change == 'bad-device': (app/'Contents/Resources/device-display.png').write_bytes((root/'assets/interface/connection-stage.png').read_bytes())
                if change == 'dirty-source': (app/'Contents/Resources/source-revision.txt').write_text(commit+' (modified)')
                if change == 'none': checker.check_app(app, '0.1.1')
                else:
                    with self.assertRaises(ValueError): checker.check_app(app, '0.1.1')

    def test_archive_validation(self):
        fixture = self.fixture()
        payload = fixture/name
        bundle(payload/'Local Remote Desktop.app')
        for doc in ('NOTICE.md', 'START-HERE.txt'): (payload/doc).write_text('fixture documentation')
        (payload/'LICENSE').write_bytes((root/'LICENSE').read_bytes())
        (payload/'SOURCE.txt').write_text(f'GPL-3.0-or-later\nSource commit: {commit}\n')
        (payload/'source.zip').write_bytes(source_archive())
        hashes = {p.relative_to(payload).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
                  for p in payload.rglob('*') if p.is_file()}
        (payload/'release.json').write_text(json.dumps({'version': '0.1.1', 'commit': commit, 'sha256': hashes}))
        for change in ('none', 'traversal', 'extra', 'symlink', 'tampered', 'wrong-commit',
                       'appledouble', 'orphan-appledouble', 'missing-license', 'missing-source',
                       'wrong-source-commit', 'wrong-source-license', 'missing-build-script', 'source-traversal'):
            with self.subTest(change=change):
                archive = fixture/(change + '.zip')
                with zipfile.ZipFile(archive, 'w') as z:
                    for path in payload.rglob('*'):
                        if path.is_file():
                            if change == 'missing-license' and path.name == 'LICENSE': continue
                            if change == 'missing-source' and path.name == 'source.zip': continue
                            content = path.read_bytes()
                            if change == 'tampered' and path.name == 'NOTICE.md': content = b'changed'
                            if change in ('wrong-source-commit', 'wrong-source-license', 'missing-build-script', 'source-traversal') and path.name == 'source.zip':
                                content = source_archive(change)
                            z.writestr(path.relative_to(fixture).as_posix(), content)
                    if change == 'traversal': z.writestr(name + '/../escape', b'fixture')
                    if change == 'extra': z.writestr(name + '/unexpected.token', b'fixture')
                    if change == 'symlink':
                        entry = zipfile.ZipInfo(name + '/link')
                        entry.external_attr = (0o120777 << 16)
                        z.writestr(entry, b'/outside')
                    if change in ('appledouble', 'orphan-appledouble'):
                        target = 'NOTICE.md' if change == 'appledouble' else 'missing'
                        z.writestr('__MACOSX/' + name + '/._' + target, b'\x00\x05\x16\x07fixture')
                if change in ('none', 'appledouble'):
                    checker.check_archive(archive, '0.1.1', commit)
                else:
                    with self.assertRaises(ValueError):
                        checker.check_archive(archive, '0.1.1', 'b'*40 if change == 'wrong-commit' else commit)

    def test_pipeline(self):
        for mode in ('ok', 'dirty', 'wrong-version', 'no-identity', 'development', 'bad-signature',
                     'no-runtime', 'no-timestamp', 'debug', 'notary-rejected', 'notary-error',
                     'staple-failed', 'extract-tampered', 'source-changed', 'existing-output'):
            with self.subTest(mode=mode):
                fixture = self.fixture()
                for directory in ('scripts', 'apps/macos', 'tests/MacStartupChecks', 'dist'):
                    (fixture/directory).mkdir(parents=True)
                bundle(fixture/'template/Local Remote Desktop.app')
                (fixture/'NOTICE.md').write_text('fixture notices')
                (fixture/'LICENSE').write_bytes((root/'LICENSE').read_bytes())
                (fixture/'fixture-source.zip').write_bytes(source_archive())
                (fixture/'apps/macos/Info.plist').write_bytes((fixture/'template/Local Remote Desktop.app/Contents/Info.plist').read_bytes())
                if mode == 'wrong-version':
                    info = plistlib.loads((fixture/'apps/macos/Info.plist').read_bytes())
                    info['CFBundleShortVersionString'] = '0.1.0'
                    (fixture/'apps/macos/Info.plist').write_bytes(plistlib.dumps(info))
                (fixture/'scripts/check-macos-package.py').write_bytes((root/'scripts/check-macos-package.py').read_bytes())
                (fixture/'apps/macos/build-app.sh').write_text('''#!/bin/bash
set -eu
[[ "$LOCALREMOTE_RELEASE_BUILD" == 1 && "$SIGN_IDENTITY" == ABCDEF ]]
echo build >> actions
mkdir -p apps/macos/build
cp -R "template/Local Remote Desktop.app" "apps/macos/build/Local Remote Desktop.app"
''', newline='\n')
                (fixture/'tests/MacStartupChecks/check.sh').write_text('echo startup-check >> actions\n', newline='\n')
                # Only archive creation/extraction is simulated by Python; production
                # manifest generation and validation run unchanged.
                (fixture/'archive.py').write_text('''import sys,zipfile
from pathlib import Path
if sys.argv[1] == 'pack':
    source, target = map(Path, sys.argv[2:])
    with zipfile.ZipFile(target, 'w') as archive:
        for path in source.rglob('*'):
            if path.is_file(): archive.write(path, path.relative_to(source.parent).as_posix())
else:
    target = str(Path(sys.argv[3]).resolve())
    if sys.platform == 'win32': target = chr(92)*2 + '?' + chr(92) + target
    with zipfile.ZipFile(sys.argv[2]) as archive: archive.extractall(target)
''', newline='\n')
                prefix = r'''
mode='MODE'
test_python='PYTHON'
export SIGN_IDENTITY=ABCDEF NOTARY_PROFILE=fixture-profile
uname() { if [[ "$1" == -s ]]; then echo Darwin; else echo arm64; fi; }
python3() {
  if [[ "$1" == tests/ReleaseChecks/check.py ]]; then echo release-check >> actions; return; fi
  command "$test_python" "$@"
}
git() {
  if [[ "$1" == archive ]]; then
    for argument in "$@"; do
      if [[ "$argument" == --output=* ]]; then cp fixture-source.zip "${argument#--output=}"; fi
    done
  elif [[ "$1" == status ]]; then
    if [[ "$mode" == dirty || ( "$mode" == source-changed && -f stapled ) ]]; then echo ' M source'; fi
  else echo aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa; fi
}
security() {
  if [[ "$mode" == development ]]; then echo '1) ABCDEF "Apple Development: Fixture"';
  elif [[ "$mode" != no-identity ]]; then echo '1) ABCDEF "Developer ID Application: Fixture"'; fi
}
codesign() {
  echo "codesign $1" >> actions
  [[ "$mode" != bad-signature ]] || return 1
  if [[ "$*" == *--verbose=4* ]]; then
    if [[ "$mode" != no-runtime ]]; then echo 'flags=0x10000(runtime)'; fi
    if [[ "$mode" != no-timestamp ]]; then echo 'Timestamp=fixture'; fi
  elif [[ "$*" == *--entitlements* && "$mode" == debug ]]; then echo 'com.apple.security.get-task-allow'; fi
  return 0
}
xcrun() {
  echo "xcrun $1 $2" >> actions
  if [[ "$1" == notarytool ]]; then
    [[ "$mode" != notary-error ]] || return 1
    if [[ "$mode" == notary-rejected ]]; then echo '{"status":"Invalid"}'; else echo '{"status":"Accepted"}'; fi
  elif [[ "$2" == staple ]]; then
    [[ "$mode" != staple-failed ]] || return 1
    touch stapled
    echo 'fixture stapled ticket' > "${@: -1}/Contents/CodeResources"
  else [[ -f "${@: -1}/Contents/CodeResources" ]]; fi
}
spctl() { echo assess >> actions; }
ditto() {
  if [[ "$1" == -c ]]; then
    echo pack >> actions
    python3 archive.py pack "${@: -2:1}" "${@: -1}"
  elif [[ "$1" == -x ]]; then
    python3 archive.py unpack "${@: -2:1}" "${@: -1}"
    if [[ "$mode" == extract-tampered ]]; then
      rm "${@: -1}/LocalRemoteDesktop-v0.1.1-macos-arm64/Local Remote Desktop.app/Contents/CodeResources"
    fi
  else cp -R "$1" "$2"; fi
}
'''.replace('MODE', mode).replace('PYTHON', Path(sys.executable).as_posix())
                source = (root/'scripts/package-macos.sh').read_text(encoding='utf-8')
                lines = source.splitlines(keepends=True)
                # Insert mocks after set -euo so production logic and failure modes run.
                fixture_script = ''.join(lines[:3]) + prefix + ''.join(lines[3:])
                script = fixture/'scripts/package-macos.sh'
                script.write_text(fixture_script, encoding='utf-8', newline='\n')
                final = fixture/'dist'/name
                if mode == 'existing-output':
                    final.mkdir()
                    (final/'existing').write_text('published artifact')
                result = subprocess.run([args.bash, str(script), '0.1.1'], capture_output=True,
                                        text=True, encoding='utf-8', timeout=40)
                self.assertEqual(result.returncode == 0, mode == 'ok', result.stdout + result.stderr)
                actions = (fixture/'actions').read_text() if (fixture/'actions').exists() else ''
                if mode == 'ok':
                    archive = final/(name + '.zip')
                    checker.check_archive(archive, '0.1.1', commit)
                    self.assertEqual((final/'SHA256SUMS.txt').read_text().split()[0], hashlib.sha256(archive.read_bytes()).hexdigest())
                    self.assertEqual(actions.count('pack\n'), 2, 'Final archive must be recreated after stapling')
                    self.assertIn('assess', actions)
                elif mode == 'existing-output': self.assertEqual((final/'existing').read_text(), 'published artifact')
                else: self.assertFalse(final.exists(), 'A rejected candidate must not create final release output')
                if mode in ('dirty', 'wrong-version', 'no-identity', 'development', 'existing-output'):
                    self.assertNotIn('startup-check', actions)
                    self.assertNotIn('build\n', actions)
                self.assertFalse(list((fixture/'dist').glob('.macos-release.*')))
                print('PASS release pipeline (mock):', mode)


unittest.main(argv=[sys.argv[0], *rest], verbosity=2)
