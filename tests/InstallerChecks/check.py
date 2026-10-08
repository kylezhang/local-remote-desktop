"""Exercise installer control flow with mock macOS commands in a disposable tree.
Never touches /Applications, TCC, launchd or real Host processes.
Usage: python tests/InstallerChecks/check.py --bash /path/to/bash --work-dir /temporary/test/directory
"""
import argparse
from pathlib import Path
import subprocess
import tempfile
import unittest

arguments = argparse.ArgumentParser()
arguments.add_argument('--bash', required=True)
arguments.add_argument('--work-dir', required=True)
options, remainder = arguments.parse_known_args()
source = (Path(__file__).resolve().parents[2] / 'macos-host/install-app.sh').read_text(encoding='utf-8')
work = Path(options.work_dir).resolve()
work.mkdir(parents=True, exist_ok=True)

class InstallerChecks(unittest.TestCase):
    def run_case(self, mode='normal', loaded=False, duplicate=False, reset=False):
        temporary = tempfile.TemporaryDirectory(prefix='installer-', dir=work)
        root = Path(temporary.name).resolve()
        self.assertTrue(root.is_relative_to(work))
        self.addCleanup(temporary.cleanup)
        apps, personal, project = root/'apps', root/'personal-apps', root/'project'
        for directory in (apps, personal, project): directory.mkdir()
        def bundle(path, old=False, identifier='com.remotemaclan.host'):
            (path/'Contents/MacOS').mkdir(parents=True)
            (path/'Contents/Info.plist').write_text(identifier, encoding='utf-8')
            executable = path/'Contents/MacOS/RemoteMacLAN'
            executable.write_text('#!/bin/sh\n# new executable\n' if not old else '#!/bin/sh\n# old executable\n', encoding='utf-8')
            executable.chmod(0o755)
            if old: (path/'Contents/obsolete-resource').write_text('must disappear')
        bundle(project/'build/RemoteMacLAN.app')
        bundle(apps/'RemoteMacLAN.app', old=True, identifier='other.app' if mode=='wrong-id' else 'com.remotemaclan.host')
        if duplicate: bundle(personal/'RemoteMacLAN.app', old=True)
        (root/'process').write_text('12345\n', newline='\n')
        (root/'agent.plist').write_text('configured agent')
        if loaded: (root/'loaded').touch()
        (project/'build-app.sh').write_text("#!/bin/bash\n[[ ! -f ../build-fail ]]\n", encoding='utf-8')
        if mode=='build-fail': (root/'build-fail').touch()
        # Rewrite only deployment paths in the COPY of the production script.
        fixture = source.replace('$HOME/Applications', '__PERSONAL__').replace('$HOME/Library/LaunchAgents/com.remotemaclan.host.plist', str(root/'agent.plist').replace('\\','/'))
        fixture = fixture.replace('/Applications', '__APPS__').replace('__APPS__', apps.as_posix()).replace('__PERSONAL__', personal.as_posix())
        fixture = fixture.replace('/usr/libexec/PlistBuddy', 'plist_read')
        self.assertNotIn('$HOME', fixture)
        self.assertNotIn('/Applications', fixture)
        prefix = r'''
_test_root='ROOT'
_mode='MODE'
uname() { echo Darwin; }
sleep() { :; }
plist_read() { if [[ "$*" == *CFBundleShortVersionString* ]]; then echo 0.1.0; else cat "${@: -1}"; fi; }
codesign() { [[ "$_mode" != bad-signature ]]; }
ditto() { cp -R "$1" "$2"; }
pgrep() { [[ -f "$_test_root/process" ]] || return 1; cat "$_test_root/process"; }
kill() {
  echo "kill $*" >> "$_test_root/actions"
  if [[ "$_mode" == stuck ]]; then return 1; fi
  if [[ "$_mode" == force && "$1" == -TERM ]]; then return 0; fi
  rm -f "$_test_root/process"
}
launchctl() {
  echo "launchctl $*" >> "$_test_root/actions"
  case "$1" in
    print) [[ -f "$_test_root/loaded" ]];;
    bootout) rm -f "$_test_root/loaded";;
    bootstrap) touch "$_test_root/loaded";;
    *) return 1;;
  esac
}
tccutil() {
  echo "tccutil $*" >> "$_test_root/actions"
  [[ "$*" == 'reset ScreenCapture com.remotemaclan.host' ]] || return 2
  [[ "$_mode" != reset-fail ]]
}
open() { echo "open $*" >> "$_test_root/actions"; }
'''.replace('ROOT', root.as_posix()).replace('MODE', mode)
        lines = fixture.splitlines(keepends=True)
        fixture = ''.join(lines[:2]) + prefix + ''.join(lines[2:])
        script = project/'install-app.sh'
        script.write_text(fixture, encoding='utf-8', newline='\n')
        command = [options.bash, str(script)]
        if reset or mode=='reset-fail': command.append('--reset-screen-permission')
        result = subprocess.run(command, capture_output=True, text=True, encoding='utf-8', timeout=30)
        actions = (root/'actions').read_text() if (root/'actions').exists() else ''
        return root, result, actions

    def test_complete_replace_and_duplicate_removal(self):
        root, result, actions = self.run_case(duplicate=True)
        self.assertEqual(result.returncode, 0, result.stdout+result.stderr)
        self.assertFalse((root/'apps/RemoteMacLAN.app/Contents/obsolete-resource').exists())
        self.assertFalse((root/'personal-apps/RemoteMacLAN.app').exists())
        self.assertEqual((root/'apps/RemoteMacLAN.app/Contents/MacOS/RemoteMacLAN').read_text(), '#!/bin/sh\n# new executable\n')
        self.assertNotIn('tccutil reset', actions)
        self.assertEqual(actions.count('open '), 1)
        self.assertFalse(list((root/'apps').glob('.remote-mac-lan-install.*')))

    def test_loaded_agent_restarts_without_second_open(self):
        root, result, actions = self.run_case(loaded=True)
        self.assertEqual(result.returncode, 0, result.stdout+result.stderr)
        self.assertTrue((root/'loaded').exists())
        self.assertIn('launchctl bootout', actions)
        self.assertIn('launchctl bootstrap', actions)
        self.assertNotIn('open ', actions)

    def test_screen_reset_is_explicit_and_scoped(self):
        _, result, actions = self.run_case(reset=True)
        self.assertEqual(result.returncode, 0, result.stdout+result.stderr)
        self.assertIn('tccutil reset ScreenCapture com.remotemaclan.host', actions)
        self.assertLess(actions.index('kill -TERM'), actions.index('tccutil reset'))
        self.assertNotIn('reset Accessibility', actions)

    def test_force_stop_of_stale_host(self):
        _, result, actions = self.run_case('force')
        self.assertEqual(result.returncode, 0, result.stdout+result.stderr)
        self.assertIn('kill -KILL', actions)

    def test_failures_preserve_old_application(self):
        for mode in ('build-fail', 'bad-signature', 'wrong-id', 'stuck', 'reset-fail'):
            with self.subTest(mode=mode):
                root, result, actions = self.run_case(mode)
                self.assertNotEqual(result.returncode, 0, result.stdout+result.stderr)
                self.assertTrue((root/'apps/RemoteMacLAN.app/Contents/obsolete-resource').exists())
                self.assertNotIn('open ', actions)
                if mode!='reset-fail': self.assertNotIn('tccutil reset', actions)
                self.assertFalse(list((root/'apps').glob('.remote-mac-lan-install.*')))

if __name__ == '__main__':
    unittest.main(argv=[__file__]+remainder, verbosity=2)
