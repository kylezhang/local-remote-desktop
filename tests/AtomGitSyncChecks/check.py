"""Headless AtomGit release-sync checks against local mock APIs; no real network."""
import contextlib
import hashlib
import http.server
import importlib.util
import io
import json
from pathlib import Path
import re
import shutil
import tempfile
import threading
import unittest
from types import SimpleNamespace
from unittest import mock
from urllib.parse import parse_qs, quote, unquote, urlparse
import os

root = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('atomgit_sync', root / 'scripts' / 'sync-atomgit-release.py')
syncmod = importlib.util.module_from_spec(spec)
spec.loader.exec_module(syncmod)
syncmod.BACKOFF_SECONDS = (0, 0)

TAG = 'v9.9.9-alpha.1'
COMMIT = 'a' * 40
OTHER_COMMIT = 'b' * 40
TOKEN = 'atomgit-test-token-4f9c2'
GITHUB_REPO = 'kylezhang/local-remote-desktop'


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def fixture_assets():
    assets = {
        f'LocalRemoteDesktop-{TAG}-windows-x64.zip': b'windows fixture bytes',
        f'LocalRemoteDesktop-{TAG}-macos-arm64-preview.zip': b'macos zip fixture bytes',
        f'LocalRemoteDesktop-{TAG}-macos-arm64-preview.dmg': b'dmg fixture bytes',
        f'LocalRemoteDesktop-{TAG}-source.zip': b'source fixture bytes',
        'release.json': json.dumps({'version': TAG[1:], 'commit': COMMIT}).encode(),
    }
    listed = sorted(name for name in assets if name != 'release.json')
    assets['SHA256SUMS.txt'] = ''.join(f'{sha256(assets[name])}  {name}\n' for name in listed).encode()
    return assets


class _Handler(http.server.BaseHTTPRequestHandler):
    def log_message(self, *args):
        pass

    def send_payload(self, status, payload):
        raw = payload if isinstance(payload, bytes) else json.dumps(payload).encode()
        self.send_response(status)
        self.send_header('Content-Type', 'application/octet-stream' if isinstance(payload, bytes) else 'application/json')
        self.send_header('Content-Length', str(len(raw)))
        self.end_headers()
        self.wfile.write(raw)

    def json_body(self):
        length = int(self.headers.get('Content-Length', 0))
        return json.loads(self.rfile.read(length) or b'{}')


class GitHubHandler(_Handler):
    def do_GET(self):
        state = self.server.state
        host = f'http://{self.server.server_address[0]}:{self.server.server_address[1]}'
        path = urlparse(self.path).path
        if path == f'/repos/{GITHUB_REPO}/releases/tags/{TAG}':
            self.send_payload(200 if state.release else 404, state.release or {'message': 'Not Found'})
        elif path == f'/repos/{GITHUB_REPO}/git/ref/tags/{TAG}':
            self.send_payload(200, {'object': {'type': 'tag', 'sha': 'c' * 40,
                                              'url': f'{host}/repos/{GITHUB_REPO}/git/tags/{"c" * 40}'}})
        elif path == f'/repos/{GITHUB_REPO}/git/tags/{"c" * 40}':
            self.send_payload(200, {'object': {'type': 'commit', 'sha': state.commit}})
        elif path.startswith('/gh-assets/'):
            name = unquote(path[len('/gh-assets/'):])
            if name not in state.assets:
                self.send_payload(404, {'message': 'Not Found'})
            else:
                self.send_payload(200, state.served.get(name, state.assets[name]))
        else:
            self.send_payload(404, {'message': 'Not Found'})


class AtomGitHandler(_Handler):
    def public_release(self, release):
        return {'tag_name': release['tag_name'], 'target_commitish': release['target_commitish'],
                'name': release['name'], 'body': release['body'], 'prerelease': release.get('prerelease', False),
                'release_status': release['release_status'], 'assets': [dict(a) for a in release['assets']]}

    def do_PUT(self):
        state = self.server.state
        path = urlparse(self.path).path
        length = int(self.headers.get('Content-Length', 0))
        data = self.rfile.read(length)
        if path.startswith('/obs/'):
            _, _, tag, raw = path.split('/', 3)
            name = unquote(raw)
            if state.corrupt_uploads:
                data += b'corrupted'
            state.blobs[(tag, name)] = data
            release = state.releases.get(tag)
            if release and not any(a['name'] == name for a in release['assets']):
                release['assets'].append({'name': name, 'type': 'attach', 'browser_download_url': ''})
            state.calls.append(('PUT', name))
            self.send_payload(200, {})
        else:
            self.send_payload(404, {'message': 'Not Found'})

    def do_GET(self):
        self.route('GET')

    def do_POST(self):
        self.route('POST')

    def do_PATCH(self):
        self.route('PATCH')

    def route(self, method):
        state = self.server.state
        parsed = urlparse(self.path)
        path, query = parsed.path, parse_qs(parsed.query)
        host = f'http://{self.server.server_address[0]}:{self.server.server_address[1]}'
        prefix = f'/api/v5/repos/{state.owner}/{state.repo}'
        if not path.startswith(prefix):
            self.send_payload(404, {'message': 'Not Found'})
            return
        if query.get('access_token', [''])[0] != state.token:
            self.send_payload(403, {'message': 'Forbidden'})
            return
        rest = path[len(prefix):]
        if rest == '/releases' and method == 'POST':
            payload = self.json_body()
            tag = payload['tag_name']
            release = {'tag_name': tag, 'name': payload.get('name') or tag, 'body': payload.get('body') or '',
                       'target_commitish': payload.get('target_commitish') or COMMIT,
                       'release_status': 'none', 'prerelease': False,
                       'assets': [{'name': f'{tag}{ext}', 'type': 'source', 'browser_download_url': ''}
                                  for ext in ('.zip', '.tar.gz', '.tar.bz2', '.tar')]}
            state.releases[tag] = release
            state.calls.append(('POST', tag))
            self.send_payload(200, self.public_release(release))
        elif rest == f'/releases/{TAG}' and method == 'GET':
            state.calls.append(('GET-RELEASE', TAG))
            release = state.releases.get(TAG)
            self.send_payload(200 if release else 404, self.public_release(release) if release else {'message': 'Not Found'})
        elif rest == f'/releases/{TAG}' and method == 'PATCH':
            release = state.releases.get(TAG)
            if not release:
                self.send_payload(404, {'message': 'Not Found'})
                return
            payload = self.json_body()
            release['name'] = payload.get('name') or release['name']
            release['body'] = payload.get('body', release['body'])
            release['release_status'] = payload.get('release_status', release['release_status'])
            state.calls.append(('PATCH', release['release_status']))
            self.send_payload(200, self.public_release(release))
        elif rest == f'/releases/{TAG}/upload_url' and method == 'GET':
            name = query.get('file_name', [''])[0]
            state.calls.append(('UPLOAD-URL', name))
            self.send_payload(200, {'url': f'{host}/obs/{TAG}/{quote(name, safe="")}',
                                    'headers': {'x-obs-meta-project-id': 'test', 'x-obs-acl': 'private',
                                                'x-obs-callback': 'test', 'Content-Type': 'application/octet-stream'}})
        elif re.fullmatch(rf'/releases/{re.escape(TAG)}/attach_files/.+/download', rest) and method == 'GET':
            name = unquote(rest.split('/')[4])
            state.calls.append(('DOWNLOAD', name))
            data = state.blobs.get((TAG, name))
            self.send_payload(200 if data is not None else 404, data if data is not None else {'message': 'Not Found'})
        else:
            self.send_payload(404, {'message': 'Not Found'})


class _Server(http.server.ThreadingHTTPServer):
    daemon_threads = True

    def __init__(self, handler, state):
        super().__init__(('127.0.0.1', 0), handler)
        self.state = state


class Checks(unittest.TestCase):
    def setUp(self):
        self.assets = fixture_assets()
        self.gh_state = SimpleNamespace(assets=self.assets, served={}, commit=COMMIT, release=None)
        self.gh = _Server(GitHubHandler, self.gh_state)
        threading.Thread(target=self.gh.serve_forever, daemon=True).start()
        self.addCleanup(self.gh.shutdown)
        self.addCleanup(self.gh.server_close)
        self.gh_base = f'http://127.0.0.1:{self.gh.server_address[1]}'
        self.gh_state.release = {
            'tag_name': TAG, 'name': TAG, 'draft': False, 'prerelease': True, 'target_commitish': COMMIT,
            'body': f'Preview notes\n\nSource commit: `{COMMIT}`\n',
            'assets': [{'name': name, 'browser_download_url': f'{self.gh_base}/gh-assets/{quote(name)}'}
                       for name in sorted(self.assets)],
        }
        self.ag_state = SimpleNamespace(owner='zkxw2008', repo='local-remote-desktop', token=TOKEN,
                                        releases={}, blobs={}, calls=[], corrupt_uploads=False)
        self.ag = _Server(AtomGitHandler, self.ag_state)
        threading.Thread(target=self.ag.serve_forever, daemon=True).start()
        self.addCleanup(self.ag.shutdown)
        self.addCleanup(self.ag.server_close)
        self.ag_base = f'http://127.0.0.1:{self.ag.server_address[1]}'
        self.work = Path(tempfile.mkdtemp())
        self.addCleanup(shutil.rmtree, self.work, True)

    def run_sync(self, prerelease=True):
        return syncmod.sync(TAG, COMMIT, prerelease,
                            github_base=self.gh_base, github_repo=GITHUB_REPO, github_token=None,
                            atomgit_base=self.ag_base, atomgit_owner=self.ag_state.owner,
                            atomgit_repo=self.ag_state.repo, atomgit_token=TOKEN, workdir=self.work)

    def seed_atomgit(self, attachments, status='none'):
        self.ag_state.releases[TAG] = {
            'tag_name': TAG, 'name': TAG, 'body': 'seeded', 'target_commitish': COMMIT,
            'release_status': status, 'prerelease': status == 'latest',
            'assets': ([{'name': f'{TAG}{ext}', 'type': 'source', 'browser_download_url': ''}
                        for ext in ('.zip', '.tar.gz', '.tar.bz2', '.tar')]
                       + [{'name': name, 'type': 'attach', 'browser_download_url': ''} for name in attachments]),
        }
        for name, data in attachments.items():
            self.ag_state.blobs[(TAG, name)] = data

    def methods(self, since=0):
        return [call[0] for call in self.ag_state.calls[since:]]

    def test_fresh_sync_creates_uploads_verifies_marks_pre(self):
        report = self.run_sync()
        self.assertEqual(sorted(report['uploaded']), sorted(self.assets))
        self.assertEqual(report['already_synced'], [])
        self.assertEqual(report['release_status'], 'pre')
        release = self.ag_state.releases[TAG]
        self.assertEqual(release['release_status'], 'pre')
        self.assertEqual(release['body'], self.gh_state.release['body'])
        for name, data in self.assets.items():
            self.assertEqual(self.ag_state.blobs[(TAG, name)], data)
        self.assertEqual([a['name'] for a in release['assets'] if a['type'] == 'source'],
                         [f'{TAG}{ext}' for ext in ('.zip', '.tar.gz', '.tar.bz2', '.tar')])
        calls = self.ag_state.calls
        last_put = max(i for i, call in enumerate(calls) if call[0] == 'PUT')
        first_patch = next(i for i, call in enumerate(calls) if call[0] == 'PATCH')
        self.assertLess(next(i for i, call in enumerate(calls) if call[0] == 'POST'), last_put)
        self.assertLess(last_put, first_patch)
        self.assertEqual(calls[first_patch], ('PATCH', 'pre'))

    def test_resync_is_idempotent(self):
        self.run_sync()
        before = len(self.ag_state.calls)
        report = self.run_sync()
        self.assertEqual(report['uploaded'], [])
        self.assertEqual(sorted(report['already_synced']), sorted(self.assets))
        self.assertEqual(report['release_status'], 'pre')
        for method in ('PUT', 'POST', 'PATCH'):
            self.assertNotIn(method, self.methods(before))

    def test_uploads_only_missing_attachment(self):
        self.seed_atomgit({name: data for name, data in self.assets.items() if name != 'release.json'})
        report = self.run_sync()
        self.assertEqual(report['uploaded'], ['release.json'])
        self.assertEqual(self.ag_state.blobs[(TAG, 'release.json')], self.assets['release.json'])
        self.assertEqual(self.ag_state.releases[TAG]['release_status'], 'pre')

    def test_checksum_conflict_halts_without_writes(self):
        conflicting = dict(self.assets)
        conflicting['release.json'] = b'tampered bytes'
        self.seed_atomgit(conflicting)
        with self.assertRaises(ValueError):
            self.run_sync()
        self.assertNotIn('PUT', self.methods())
        self.assertNotIn('PATCH', self.methods())
        self.assertEqual(self.ag_state.releases[TAG]['release_status'], 'none')
        self.assertEqual(self.ag_state.blobs[(TAG, 'release.json')], b'tampered bytes')

    def test_corrupted_upload_halts_before_status(self):
        self.ag_state.corrupt_uploads = True
        with self.assertRaises(ValueError):
            self.run_sync()
        self.assertNotIn('PATCH', self.methods())
        self.assertEqual(self.ag_state.releases[TAG]['release_status'], 'none')

    def test_draft_github_release_refused(self):
        self.gh_state.release['draft'] = True
        with self.assertRaises(ValueError):
            self.run_sync()
        self.assertEqual(self.ag_state.calls, [])

    def test_prerelease_flag_mismatch_refused(self):
        self.gh_state.release['prerelease'] = False
        with self.assertRaises(ValueError):
            self.run_sync()
        self.assertEqual(self.ag_state.calls, [])

    def test_tag_commit_mismatch_refused(self):
        self.gh_state.commit = OTHER_COMMIT
        with self.assertRaises(ValueError):
            self.run_sync()
        self.assertEqual(self.ag_state.calls, [])

    def test_atomgit_commit_mismatch_refused(self):
        self.seed_atomgit(self.assets)
        self.ag_state.releases[TAG]['target_commitish'] = OTHER_COMMIT
        with self.assertRaises(ValueError):
            self.run_sync()
        self.assertNotIn('PUT', self.methods())

    def test_github_asset_fails_own_checksum(self):
        self.gh_state.served[f'LocalRemoteDesktop-{TAG}-source.zip'] = b'corrupted in transit'
        with self.assertRaises(ValueError):
            self.run_sync()
        self.assertEqual(self.ag_state.calls, [])

    def main_argv(self):
        return ['--tag', TAG, '--commit', COMMIT, '--work-dir', str(self.work),
                '--github-base', self.gh_base, '--github-repo', GITHUB_REPO,
                '--atomgit-base', self.ag_base, '--atomgit-owner', self.ag_state.owner,
                '--atomgit-repo', self.ag_state.repo]

    def test_token_never_leaks(self):
        env = dict(os.environ)
        env.pop('GITHUB_ACTIONS', None)
        env['ATOMGIT_RELEASE_TOKEN'] = TOKEN
        out, err = io.StringIO(), io.StringIO()
        with mock.patch.dict(os.environ, env, clear=True), \
                contextlib.redirect_stdout(out), contextlib.redirect_stderr(err):
            self.assertEqual(syncmod.main(self.main_argv()), 0)
        combined = out.getvalue() + err.getvalue() + (self.work / 'atomgit-sync-report.json').read_text()
        self.assertNotIn(TOKEN, combined)
        self.assertEqual(self.ag_state.releases[TAG]['release_status'], 'pre')

    def test_main_conflict_reports_failure_without_token_leak(self):
        conflicting = dict(self.assets)
        conflicting['release.json'] = b'tampered bytes'
        self.seed_atomgit(conflicting)
        env = dict(os.environ)
        env.pop('GITHUB_ACTIONS', None)
        env['ATOMGIT_RELEASE_TOKEN'] = TOKEN
        out, err = io.StringIO(), io.StringIO()
        with mock.patch.dict(os.environ, env, clear=True), \
                contextlib.redirect_stdout(out), contextlib.redirect_stderr(err):
            self.assertEqual(syncmod.main(self.main_argv()), 1)
        self.assertIn('immutable', err.getvalue())
        self.assertNotIn(TOKEN, out.getvalue() + err.getvalue())

    def test_main_ci_mirrors_published_tag_when_checkout_moves_on(self):
        env = {'GITHUB_ACTIONS': 'true', 'GITHUB_REPOSITORY': GITHUB_REPO,
               'GITHUB_REF': 'refs/heads/main', 'ATOMGIT_RELEASE_TOKEN': TOKEN,
               'GITHUB_SHA': OTHER_COMMIT}
        argv = ['--work-dir', str(self.work),
                '--github-base', self.gh_base, '--github-repo', GITHUB_REPO,
                '--atomgit-base', self.ag_base, '--atomgit-owner', self.ag_state.owner,
                '--atomgit-repo', self.ag_state.repo]
        out, err = io.StringIO(), io.StringIO()
        with mock.patch.dict(os.environ, env, clear=True), \
                mock.patch.object(syncmod, 'derive_preview_tag', return_value=TAG), \
                contextlib.redirect_stdout(out), contextlib.redirect_stderr(err):
            self.assertEqual(syncmod.main(argv), 0)
        self.assertIn('Mirroring the published', out.getvalue())
        self.assertNotIn(TOKEN, out.getvalue() + err.getvalue())
        self.assertEqual(self.ag_state.releases[TAG]['release_status'], 'pre')
        self.assertEqual(sorted(name for (_, name) in self.ag_state.blobs), sorted(self.assets))

    def test_main_skips_without_token_in_ci(self):
        env = {'GITHUB_ACTIONS': 'true', 'GITHUB_REPOSITORY': GITHUB_REPO,
               'GITHUB_REF': 'refs/heads/main', 'ATOMGIT_RELEASE_TOKEN': ''}
        out = io.StringIO()
        with mock.patch.dict(os.environ, env, clear=True), contextlib.redirect_stdout(out):
            self.assertEqual(syncmod.main(self.main_argv()), 0)
        self.assertIn('skipping', out.getvalue())
        self.assertEqual(self.ag_state.calls, [])

    def test_main_skips_foreign_repository(self):
        env = {'GITHUB_ACTIONS': 'true', 'GITHUB_REPOSITORY': 'someone/fork',
               'GITHUB_REF': 'refs/heads/main', 'ATOMGIT_RELEASE_TOKEN': TOKEN}
        out = io.StringIO()
        with mock.patch.dict(os.environ, env, clear=True), contextlib.redirect_stdout(out):
            self.assertEqual(syncmod.main(self.main_argv()), 0)
        self.assertIn('skipping', out.getvalue())
        self.assertEqual(self.ag_state.calls, [])


if __name__ == '__main__':
    unittest.main()
