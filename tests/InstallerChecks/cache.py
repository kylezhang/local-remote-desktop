"""Build-cache relocation checks using mock Apple tools, never installed apps."""
import argparse
from pathlib import Path
import subprocess
import tempfile
import unittest

parser = argparse.ArgumentParser()
parser.add_argument('--bash', required=True)
parser.add_argument('--work-dir', required=True)
args, rest = parser.parse_known_args()
source = Path(__file__).resolve().parents[2] / 'apps/macos/build-app.sh'
work = Path(args.work_dir).resolve()
work.mkdir(parents=True, exist_ok=True)


class BuildCacheChecks(unittest.TestCase):
    def fixture(self):
        temporary = tempfile.TemporaryDirectory(prefix='cache-', dir=work)
        self.addCleanup(temporary.cleanup)
        parent = Path(temporary.name).resolve()
        self.assertTrue(parent.is_relative_to(work))
        project = parent / 'project'
        project.mkdir()
        for name in ('Info.plist', 'LocalRemoteDesktop.icns', 'icon.png', 'strings.json', 'initiate-connection.png', 'accept-connection.png'):
            (project / name).write_text('fixture resource', encoding='utf-8')
        old = project / 'build/Local Remote Desktop.app/Contents/old-resource'
        old.parent.mkdir(parents=True)
        old.write_text('previous complete bundle', encoding='utf-8')
        script = source.read_text(encoding='utf-8')
        prefix = '''
unset SIGN_IDENTITY LOCALREMOTE_REQUIRE_STABLE_SIGNATURE LOCALREMOTE_RELEASE_BUILD RML_REQUIRE_STABLE_SIGNATURE RML_RELEASE_BUILD
uname() { echo arm64; }
security() { :; }
codesign() { :; }
xcrun() {
  if [[ "$*" == *--show-bin-path* ]]; then echo "$PWD/bin"; return; fi
  echo build >> "$PWD/actions"
  [[ ! -f "$PWD/fail-build" ]] || return 42
  local pcm="$PWD/.build-native/ModuleCache/fixture.pcm"
  if [[ -f "$pcm" && "$(cat "$pcm")" != "$PWD" ]]; then
    echo 'precompiled file was compiled with another module cache path' >&2; return 1
  fi
  mkdir -p "$PWD/bin" "$(dirname "$pcm")"
  printf '%s\\n' "$PWD" > "$pcm"
  printf 'mock executable\\n' > "$PWD/bin/LocalRemoteDesktop"
}
'''
        lines = script.splitlines(keepends=True)
        fixture = ''.join(lines[:2]) + prefix + ''.join(lines[2:])
        fixture = fixture.replace('../../assets/brand/LocalRemoteDesktop.icns', 'LocalRemoteDesktop.icns')
        fixture = fixture.replace('../../assets/brand/icon.png', 'icon.png')
        fixture = fixture.replace('../../assets/localization/strings.json', 'strings.json')
        fixture = fixture.replace('../../assets/interface/', '')
        (project / 'build-app.sh').write_text(fixture, encoding='utf-8', newline='\n')
        return parent, project

    def run_build(self, project, success=True):
        result = subprocess.run([args.bash, str(project / 'build-app.sh')],
                                capture_output=True, text=True, encoding='utf-8', timeout=20)
        self.assertEqual(result.returncode == 0, success, result.stdout + result.stderr)
        return result

    def seed_cache(self, project, marker=None):
        cache = project / '.build-native'
        (cache / 'ModuleCache').mkdir(parents=True)
        (cache / 'ModuleCache/fixture.pcm').write_text('/old/checkout', encoding='utf-8')
        if marker is not None:
            (cache / '.localremote-project-path').write_text(marker + '\n', encoding='utf-8')
        return cache

    def test_unknown_or_moved_cache_rebuilt(self):
        for marker in (None, '/previous/project'):
            with self.subTest(marker=marker):
                _, project = self.fixture()
                cache = self.seed_cache(project, marker)
                self.run_build(project)
                self.assertEqual((cache / 'ModuleCache/fixture.pcm').read_text().strip(), project.as_posix())
                self.assertEqual((cache / '.localremote-project-path').read_text().strip(), project.as_posix())

    def test_rename_then_incremental_reuse(self):
        parent, project = self.fixture()
        self.run_build(project)
        renamed = parent / 'renamed project'
        project.rename(renamed)
        self.run_build(renamed)
        cache = renamed / '.build-native'
        sentinel = cache / 'incremental-product'
        sentinel.write_text('keep', encoding='utf-8')
        self.run_build(renamed)
        self.assertEqual(sentinel.read_text(), 'keep')
        self.assertEqual((cache / 'ModuleCache/fixture.pcm').read_text().strip(), renamed.as_posix())

    def test_build_failure_preserves_bundle_and_settings(self):
        _, project = self.fixture()
        self.seed_cache(project)
        (project / 'fail-build').touch()
        (project / '.signing-identity').write_text('fixture-identity', encoding='utf-8')
        self.run_build(project, False)
        self.assertTrue((project / 'build/Local Remote Desktop.app/Contents/old-resource').is_file())
        self.assertEqual((project / '.signing-identity').read_text(), 'fixture-identity')

    def test_unsafe_cache_paths_refused(self):
        for kind in ('directory-link', 'marker-link', 'regular-file'):
            with self.subTest(kind=kind):
                parent, project = self.fixture()
                outside = parent / 'outside'
                outside.mkdir()
                sentinel = outside / 'keep'
                sentinel.write_text('untouched', encoding='utf-8')
                cache = project / '.build-native'
                if kind == 'directory-link':
                    cache.symlink_to(outside, target_is_directory=True)
                elif kind == 'marker-link':
                    cache.mkdir()
                    (cache / '.localremote-project-path').symlink_to(sentinel)
                else:
                    cache.write_text('unexpected file', encoding='utf-8')
                self.run_build(project, False)
                self.assertEqual(sentinel.read_text(), 'untouched')
                self.assertFalse((project / 'actions').exists())
                self.assertTrue((project / 'build/Local Remote Desktop.app/Contents/old-resource').exists())


if __name__ == '__main__':
    unittest.main(argv=['cache.py'] + rest)
