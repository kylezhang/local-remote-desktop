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
    def run_case(self, mode='normal', loaded=False, duplicate=False, option=None):
        temporary = tempfile.TemporaryDirectory(prefix='installer-', dir=work)
        root = Path(temporary.name).resolve()
        self.assertTrue(root.is_relative_to(work))
        self.addCleanup(temporary.cleanup)
        apps, personal, project = root/'apps', root/'personal-apps', root/'project'
        for directory in (apps, personal, project): directory.mkdir()
        def bundle(path, old=False, identifier='com.remotemaclan.host'):
            (path/'Contents/MacOS').mkdir(parents=True)
            (path/'Contents/Info.plist').write_text(identifier, encoding='utf-8')
            (path/'Contents/Resources').mkdir()
            for name in ('LocalRemote.icns', 'LocalRemote.png'):
                (path/'Contents/Resources'/name).write_text('new icon' if not old else 'old icon')
            executable = path/'Contents/MacOS/RemoteMacLAN'
            executable.write_text('#!/bin/sh\n# new executable\n' if not old else '#!/bin/sh\n# old executable\n', encoding='utf-8')
            executable.chmod(0o755)
            if old: (path/'Contents/obsolete-resource').write_text('must disappear')
        bundle(project/'build/RemoteMacLAN.app')
        if mode=='missing-icon': (project/'build/RemoteMacLAN.app/Contents/Resources/LocalRemote.png').unlink()
        if mode != 'first-install-verification-fail':
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
plist_read() {
  if [[ "$*" == *CFBundleShortVersionString* ]]; then
    [[ "$_mode" != installed-metadata-fail ]] || return 1
    echo 0.1.0
  elif [[ "$*" == *CFBundleIconFile* ]]; then
    if [[ "$_mode" == bad-icon-metadata ]]; then echo wrong.icns; else echo LocalRemote.icns; fi
  else cat "${@: -1}"; fi
}
codesign() {
  [[ "$_mode" != bad-signature ]] || return 1
  if [[ "$1" == -d ]]; then
    [[ "$_mode" != unreadable-requirement ]] || return 1
    if [[ "$_mode" == changed-adhoc && "${@: -1}" == */build/RemoteMacLAN.app ]]; then
      echo 'designated => cdhash H"new"'
    elif [[ "$_mode" == changed-adhoc ]]; then
      echo 'designated => cdhash H"old"'
    elif [[ "$_mode" == changed-user-identity && "${@: -1}" == */personal-apps/RemoteMacLAN.app ]]; then
      echo 'designated => identifier "com.remotemaclan.host" and anchor other'
    elif [[ "$_mode" == unchanged-adhoc ]]; then
      echo 'designated => cdhash H"same"'
    else
      echo 'designated => identifier "com.remotemaclan.host" and anchor fixture'
    fi
    return 0
  fi
  if [[ "$*" == *' -R '* ]]; then
    echo "requirement-verify ${@: -1}" >> "$_test_root/actions"
    [[ "$_mode" != requirement-rejected ]] || return 1
    return 0
  fi
  if [[ "${@: -1}" == "$_test_root/apps/RemoteMacLAN.app" ]]; then
    case "$_mode" in
      installed-signature-fail|restore-fail|first-install-verification-fail) return 1;;
      interrupted-install) builtin kill -TERM $$; return 1;;
    esac
  fi
}
mv() {
  if [[ "$2" == "$_test_root/apps/RemoteMacLAN.app" && "$1" == */RemoteMacLAN.app && "$_mode" == install-move-fail ]]; then return 1; fi
  if [[ "$1" == */previous-system.app && "$_mode" == restore-fail ]]; then return 1; fi
  if [[ "$1" == "$_test_root/personal-apps/RemoteMacLAN.app" && "$_mode" == user-backup-fail ]]; then return 1; fi
  command mv "$@"
}
ditto() {
  cp -R "$1" "$2"
  if [[ "$_mode" == bad-stage-icon ]]; then echo tampered > "$2/Contents/Resources/LocalRemote.png"; fi
}
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
    bootstrap) [[ "$_mode" != launch-fail ]] || return 1; touch "$_test_root/loaded";;
    *) return 1;;
  esac
}
tccutil() {
  echo "tccutil $*" >> "$_test_root/actions"
  case "$*" in
    'reset ScreenCapture com.remotemaclan.host') [[ "$_mode" != reset-fail ]] ;;
    'reset Accessibility com.remotemaclan.host') [[ "$_mode" != accessibility-reset-fail ]] ;;
    *) return 2 ;;
  esac
}
open() { echo "open $*" >> "$_test_root/actions"; [[ "$_mode" != launch-fail ]]; }
'''.replace('ROOT', root.as_posix()).replace('MODE', mode)
        lines = fixture.splitlines(keepends=True)
        fixture = ''.join(lines[:2]) + prefix + ''.join(lines[2:])
        script = project/'install-app.sh'
        script.write_text(fixture, encoding='utf-8', newline='\n')
        command = [options.bash, str(script)]
        if option: command.append(option)
        result = subprocess.run(command, capture_output=True, text=True, encoding='utf-8', timeout=30)
        actions = (root/'actions').read_text() if (root/'actions').exists() else ''
        return root, result, actions

    def test_complete_replace_and_duplicate_removal(self):
        root, result, actions = self.run_case(duplicate=True)
        self.assertEqual(result.returncode, 0, result.stdout+result.stderr)
        self.assertFalse((root/'apps/RemoteMacLAN.app/Contents/obsolete-resource').exists())
        self.assertFalse((root/'personal-apps/RemoteMacLAN.app').exists())
        self.assertEqual((root/'apps/RemoteMacLAN.app/Contents/MacOS/RemoteMacLAN').read_text(), '#!/bin/sh\n# new executable\n')
        self.assertIn('tccutil reset ScreenCapture com.remotemaclan.host', actions)
        self.assertIn('tccutil reset Accessibility com.remotemaclan.host', actions)
        self.assertEqual((root/'apps/RemoteMacLAN.app/Contents/Resources/LocalRemote.png').read_text(), 'new icon')
        self.assertEqual(actions.count('open '), 1)
        self.assertFalse(list((root/'apps').glob('.remote-mac-lan-install.*')))

    def test_loaded_agent_restarts_without_second_open(self):
        root, result, actions = self.run_case(loaded=True)
        self.assertEqual(result.returncode, 0, result.stdout+result.stderr)
        self.assertTrue((root/'loaded').exists())
        self.assertIn('launchctl bootout', actions)
        self.assertIn('launchctl bootstrap', actions)
        self.assertNotIn('open ', actions)

    def test_default_resets_both_services_only_for_our_bundle(self):
        _, result, actions = self.run_case()
        self.assertEqual(result.returncode, 0, result.stdout+result.stderr)
        resets=[line for line in actions.splitlines() if line.startswith('tccutil ')]
        self.assertEqual(resets, ['tccutil reset ScreenCapture com.remotemaclan.host', 'tccutil reset Accessibility com.remotemaclan.host'])
        self.assertLess(actions.index('kill -TERM'), actions.index('tccutil reset'))
        self.assertLess(actions.index('tccutil reset Accessibility'), actions.index('open '))

    def test_permission_preservation_is_available(self):
        _, result, actions = self.run_case(option='--keep-permissions')
        self.assertEqual(result.returncode, 0, result.stdout+result.stderr)
        self.assertNotIn('tccutil ', actions)
        self.assertEqual(actions.count('requirement-verify '), 2)

    def test_incompatible_preservation_leaves_working_host_and_permissions_untouched(self):
        for mode in ('changed-adhoc', 'changed-user-identity', 'unreadable-requirement', 'requirement-rejected'):
            with self.subTest(mode=mode):
                root, result, actions = self.run_case(mode, loaded=True, duplicate=True, option='--keep-permissions')
                self.assertNotEqual(result.returncode, 0, result.stdout+result.stderr)
                for folder in ('apps', 'personal-apps'):
                    self.assertTrue((root/folder/'RemoteMacLAN.app/Contents/obsolete-resource').exists())
                self.assertTrue((root/'process').exists())
                self.assertTrue((root/'loaded').exists())
                for forbidden in ('kill ', 'launchctl ', 'tccutil ', 'open '):
                    self.assertNotIn(forbidden, actions)
                self.assertFalse(list((root/'apps').glob('.remote-mac-lan-install.*')))

    def test_unchanged_adhoc_can_preserve_but_changed_reset_is_explicit(self):
        _, result, actions = self.run_case('unchanged-adhoc', option='--keep-permissions')
        self.assertEqual(result.returncode, 0, result.stdout+result.stderr)
        self.assertNotIn('tccutil ', actions)
        _, result, actions = self.run_case('changed-adhoc', option='--reset-permissions')
        self.assertEqual(result.returncode, 0, result.stdout+result.stderr)
        self.assertEqual(actions.count('tccutil '), 2)

    def test_explicit_reset_alias(self):
        _, result, actions = self.run_case(option='--reset-permissions')
        self.assertEqual(result.returncode, 0, result.stdout+result.stderr)
        self.assertIn('tccutil reset Accessibility com.remotemaclan.host', actions)

    def test_legacy_screen_only_option_is_scoped(self):
        _, result, actions = self.run_case(option='--reset-screen-permission')
        self.assertEqual(result.returncode, 0, result.stdout+result.stderr)
        self.assertIn('tccutil reset ScreenCapture com.remotemaclan.host', actions)
        self.assertLess(actions.index('kill -TERM'), actions.index('tccutil reset'))
        self.assertNotIn('reset Accessibility', actions)

    def test_force_stop_of_stale_host(self):
        _, result, actions = self.run_case('force')
        self.assertEqual(result.returncode, 0, result.stdout+result.stderr)
        self.assertIn('kill -KILL', actions)

    def test_failures_preserve_old_application(self):
        for mode in ('build-fail', 'bad-signature', 'wrong-id', 'stuck', 'reset-fail', 'accessibility-reset-fail', 'missing-icon', 'bad-stage-icon', 'bad-icon-metadata'):
            with self.subTest(mode=mode):
                root, result, actions = self.run_case(mode)
                self.assertNotEqual(result.returncode, 0, result.stdout+result.stderr)
                self.assertTrue((root/'apps/RemoteMacLAN.app/Contents/obsolete-resource').exists())
                self.assertNotIn('open ', actions)
                if mode not in ('reset-fail','accessibility-reset-fail'): self.assertNotIn('tccutil reset', actions)
                self.assertFalse(list((root/'apps').glob('.remote-mac-lan-install.*')))

    def test_late_failures_restore_both_complete_bundles(self):
        for mode in ('install-move-fail', 'installed-signature-fail', 'installed-metadata-fail', 'interrupted-install', 'user-backup-fail'):
            with self.subTest(mode=mode):
                root, result, actions = self.run_case(mode, duplicate=True, loaded=True, option='--keep-permissions')
                self.assertNotEqual(result.returncode, 0, result.stdout+result.stderr)
                for folder in ('apps', 'personal-apps'):
                    old = root/folder/'RemoteMacLAN.app/Contents'
                    self.assertTrue((old/'obsolete-resource').exists(), result.stdout+result.stderr)
                    self.assertEqual((old/'Resources/LocalRemote.png').read_text(), 'old icon')
                self.assertNotIn('open ', actions)
                self.assertNotIn('launchctl bootstrap', actions)
                self.assertFalse(list((root/'apps').glob('.remote-mac-lan-install.*')))

    def test_failed_restore_retains_recoverable_backup(self):
        root, result, actions = self.run_case('restore-fail')
        self.assertNotEqual(result.returncode, 0)
        stages=list((root/'apps').glob('.remote-mac-lan-install.*'))
        self.assertEqual(len(stages), 1)
        self.assertTrue((stages[0]/'previous-system.app/Contents/obsolete-resource').exists())
        self.assertIn('请勿删除', result.stderr)
        self.assertNotIn('open ', actions)

    def test_rollback_does_not_repeat_or_claim_to_restore_permissions(self):
        root, result, actions = self.run_case('installed-signature-fail')
        self.assertNotEqual(result.returncode, 0)
        resets=[line for line in actions.splitlines() if line.startswith('tccutil ')]
        self.assertEqual(resets, ['tccutil reset ScreenCapture com.remotemaclan.host', 'tccutil reset Accessibility com.remotemaclan.host'])
        self.assertTrue((root/'apps/RemoteMacLAN.app/Contents/obsolete-resource').exists())
        self.assertIn('权限不会回滚', result.stderr)

    def test_failed_first_install_removes_unverified_bundle(self):
        root, result, actions = self.run_case('first-install-verification-fail')
        self.assertNotEqual(result.returncode, 0)
        self.assertFalse((root/'apps/RemoteMacLAN.app').exists())
        self.assertFalse(list((root/'apps').glob('.remote-mac-lan-install.*')))
        self.assertNotIn('open ', actions)

    def test_launch_failure_keeps_verified_new_bundle(self):
        for loaded in (False, True):
            with self.subTest(loaded=loaded):
                root, result, actions = self.run_case('launch-fail', loaded=loaded)
                self.assertNotEqual(result.returncode, 0)
                app=root/'apps/RemoteMacLAN.app/Contents'
                self.assertFalse((app/'obsolete-resource').exists())
                self.assertEqual((app/'Resources/LocalRemote.png').read_text(), 'new icon')
                self.assertFalse(list((root/'apps').glob('.remote-mac-lan-install.*')))

if __name__ == '__main__':
    unittest.main(argv=[__file__]+remainder, verbosity=2)
