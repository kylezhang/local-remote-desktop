"""Signing control-flow checks with mocked macOS tools; no real keys or TCC."""
import argparse
import subprocess
import tempfile
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('--bash', required=True)
parser.add_argument('--work-dir', required=True)
args = parser.parse_args()
source = Path(__file__).resolve().parents[2] / 'apps/macos'
work = Path(args.work_dir).resolve()
work.mkdir(parents=True, exist_ok=True)

for mode, script_name, success in [
    ('none', 'configure-signing.sh', False),
    ('certificate', 'configure-signing.sh', True),
    ('stable-missing', 'build-app.sh', False),
    ('invalid-saved', 'build-app.sh', False),
    ('certificate', 'build-app.sh', True),
    ('adhoc', 'build-app.sh', True),
    ('release-certificate', 'build-app.sh', True),
    ('release-development', 'build-app.sh', False),
    ('release-missing', 'build-app.sh', False),
    ('sign-failed', 'build-app.sh', False),
    ('move-failed', 'build-app.sh', False),
    ('interrupted-build', 'build-app.sh', False),
    ('legacy-pinned', 'build-app.sh', True),
]:
    with tempfile.TemporaryDirectory(prefix='signing-', dir=work) as temporary:
        root = Path(temporary).resolve()
        assert root.is_relative_to(work)
        (root/'bin').mkdir()
        (root/'bin/LocalRemote').write_text('mock executable')
        (root/'Info.plist').write_text('mock bundle metadata')
        (root/'LocalRemote.icns').write_text('mock icon')
        (root/'icon.png').write_text('mock PNG icon')
        (root/'strings.json').write_text('{}')
        if mode == 'stable-missing': (root/'.require-stable-signature').touch()
        if mode == 'invalid-saved': (root/'.signing-identity').write_text('INVALID')
        if mode == 'legacy-pinned':
            (root/'legacy-signing').mkdir()
            (root/'legacy-signing/.signing-identity').write_text('ABCDEF')
            (root/'legacy-signing/.require-stable-signature').touch()
        old = root/'build/LocalRemote.app/Contents/obsolete-resource'
        old.parent.mkdir(parents=True)
        old.write_text('old bundle')
        prefix = '''
unset SIGN_IDENTITY LOCALREMOTE_REQUIRE_STABLE_SIGNATURE LOCALREMOTE_RELEASE_BUILD RML_REQUIRE_STABLE_SIGNATURE RML_RELEASE_BUILD
uname() { if [[ "$1" == -s ]]; then echo Darwin; else echo arm64; fi; }
security() {
  if [[ MODE == certificate || MODE == release-certificate || MODE == sign-failed ]]; then echo '1) ABCDEF "Developer ID Application: Fixture"'; fi
  if [[ MODE == release-development ]]; then echo '1) ABCDEF "Apple Development: Fixture"'; fi
}
codesign() { echo "codesign $*" >> "$PWD/actions"; [[ "$*" != *INVALID* && MODE != sign-failed ]]; }
xcrun() { echo 'build' >> "$PWD/actions"; if [[ "$*" == *--show-bin-path* ]]; then echo "$PWD/bin"; fi; }
mv() {
  if [[ "$2" == "$PWD/build/LocalRemote.app" && "$1" == */.bundle.*/LocalRemote.app ]]; then
    if [[ MODE == move-failed ]]; then return 1; fi
    if [[ MODE == interrupted-build ]]; then builtin kill -TERM $$; return 1; fi
  fi
  command mv "$@"
}
'''.replace('MODE', mode)
        if mode.startswith('release-'):
            prefix += '\nexport LOCALREMOTE_RELEASE_BUILD=1 SIGN_IDENTITY=ABCDEF\n'
        script = (source/script_name).read_text(encoding='utf-8')
        lines = script.splitlines(keepends=True)
        fixture = ''.join(lines[:2]) + prefix + ''.join(lines[2:])
        # Probe source is disposable fixture data, not any installed executable.
        fixture = fixture.replace('/usr/bin/true', '"$PWD/bin/LocalRemote"')
        fixture = fixture.replace('$PWD/../../macos-host', '$PWD/legacy-signing')
        fixture = fixture.replace('../../assets/brand/LocalRemote.icns', 'LocalRemote.icns')
        fixture = fixture.replace('../../assets/brand/icon.png', 'icon.png')
        fixture = fixture.replace('../../assets/localization/strings.json', 'strings.json')
        path = root/script_name
        path.write_text(fixture, encoding='utf-8', newline='\n')
        result = subprocess.run([args.bash, str(path)], capture_output=True, text=True, encoding='utf-8', timeout=20)
        assert (result.returncode == 0) == success, result.stdout + result.stderr
        actions = (root/'actions').read_text() if (root/'actions').exists() else ''
        if mode == 'stable-missing': assert not actions
        if mode == 'invalid-saved': assert '--sign - ' not in actions
        if mode in {'certificate', 'legacy-pinned'}:
            assert (root/'.signing-identity').read_text().strip() == 'ABCDEF'
            assert (root/'.require-stable-signature').exists()
            if script_name == 'build-app.sh':
                assert (root/'build/LocalRemote.app/Contents/Resources/signing-mode.txt').read_text().strip() == 'certificate'
        if mode == 'none': assert not (root/'.signing-identity').exists()
        if mode == 'adhoc':
            assert not (root/'.require-stable-signature').exists()
            assert (root/'build/LocalRemote.app/Contents/Resources/signing-mode.txt').read_text().strip() == 'ad-hoc'
        if mode == 'release-certificate':
            assert '--options runtime --timestamp' in actions
            assert not (root/'.signing-identity').exists()
            assert not (root/'.require-stable-signature').exists()
        if mode in {'release-development', 'release-missing'}: assert not actions
        if script_name == 'build-app.sh':
            assert old.exists() != success, 'Old bundle must survive failure; stale resources must not survive success'
            assert not list((root/'build').glob('.bundle.*'))
        assert not list(root.glob('.signing-probe.*'))
        print(f'PASS {script_name}: {mode}')
