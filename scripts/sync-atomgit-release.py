"""Mirror verified GitHub release bytes to AtomGit; only add missing attachments."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
GITHUB_ACCEPT = 'application/vnd.github+json'
BACKOFF_SECONDS = (2, 5)


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def http(url, token=None, *, method='GET', data=None, headers=None, accept=None, attempts=3, timeout=300):
    last = None
    for attempt in range(attempts):
        request = urllib.request.Request(url, data=data, method=method)
        if token:
            request.add_header('Authorization', f'Bearer {token}')
        if accept:
            request.add_header('Accept', accept)
        for key, value in (headers or {}).items():
            request.add_header(key, value)
        try:
            with urllib.request.urlopen(request, timeout=timeout) as response:
                return response.status, response.read()
        except urllib.error.HTTPError as error:
            if error.code < 500 and error.code != 429:
                return error.code, error.read()
            last = f'HTTP {error.code}'
        except OSError as error:
            last = str(error)
        if attempt + 1 < attempts:
            time.sleep(BACKOFF_SECONDS[min(attempt, len(BACKOFF_SECONDS) - 1)])
    raise ConnectionError(f'{method} {url.split("?", 1)[0]} failed after {attempts} attempts: {last}')


def atomgit_api(base, owner, repo, path, token, *, method='GET', payload=None, query=None, expect=(200,)):
    params = {'access_token': token, **(query or {})}
    url = f'{base}/api/v5/repos/{owner}/{repo}{path}?{urllib.parse.urlencode(params)}'
    data = json.dumps(payload).encode() if payload is not None else None
    headers = {'Content-Type': 'application/json'} if data is not None else None
    status, body = http(url, method=method, data=data, headers=headers, accept='application/json')
    if status not in expect:
        detail = body.decode('utf-8', 'replace')[:400].replace(token, '***')
        raise RuntimeError(f'AtomGit API {method} {path} returned HTTP {status}: {detail}')
    return status, body


def release_path(tag):
    return f'/releases/{urllib.parse.quote(tag, safe="")}'


def fetch_github_release(base, repo, tag, token):
    status, body = http(f'{base}/repos/{repo}/releases/tags/{tag}', token, accept=GITHUB_ACCEPT)
    if status == 404:
        raise LookupError(f'GitHub release {tag} does not exist; publish it first')
    if status != 200:
        raise RuntimeError(f'GitHub release lookup returned HTTP {status}')
    return json.loads(body)


def fetch_tag_commit(base, repo, tag, token):
    status, body = http(f'{base}/repos/{repo}/git/ref/tags/{tag}', token, accept=GITHUB_ACCEPT)
    if status != 200:
        raise LookupError(f'GitHub tag {tag} lookup returned HTTP {status}')
    reference = json.loads(body)['object']
    if reference['type'] == 'tag':
        status, body = http(reference['url'], token, accept=GITHUB_ACCEPT)
        if status != 200:
            raise LookupError(f'GitHub annotated tag {tag} lookup returned HTTP {status}')
        reference = json.loads(body)['object']
    if reference['type'] != 'commit':
        raise ValueError(f'GitHub tag {tag} does not resolve to a commit')
    return reference['sha']


def sync(tag, commit, prerelease, *, github_base, github_repo, github_token,
         atomgit_base, atomgit_owner, atomgit_repo, atomgit_token, workdir, atomgit_status=None):
    staged = Path(workdir) / 'github'
    staged.mkdir(parents=True, exist_ok=True)
    release = fetch_github_release(github_base, github_repo, tag, github_token)
    if release.get('tag_name') != tag:
        raise ValueError('GitHub release tag differs from the requested tag')
    if release.get('draft'):
        raise ValueError('GitHub release is still a draft; refusing to sync')
    if bool(release.get('prerelease')) != prerelease:
        raise ValueError('GitHub prerelease flag disagrees with the requested release_status')
    if fetch_tag_commit(github_base, github_repo, tag, github_token) != commit:
        raise ValueError(f'GitHub tag {tag} does not point at commit {commit}')
    archive_names = {f'LocalRemoteDesktop-{tag}-{suffix}' for suffix in (
        'windows-x64.zip', 'macos-arm64-preview.zip', 'macos-arm64-preview.dmg', 'source.zip')}
    required = archive_names | {'SHA256SUMS.txt', 'release.json'}
    assets = {}
    for asset in release.get('assets', []):
        name = asset.get('name', '')
        if name not in required or name in assets:
            raise ValueError('Unexpected or duplicate GitHub attachment')
        if not re.fullmatch(r'sha256:[a-f0-9]{64}', asset.get('digest', '')):
            raise ValueError(f'GitHub API digest missing or invalid for {name}')
        assets[name] = asset
    if set(assets) != required:
        raise ValueError('GitHub release lacks required package, source or verification attachments')
    digests = {}
    for name, asset in sorted(assets.items()):
        print(f'Verifying GitHub attachment: {name}', flush=True)
        status, data = http(asset['browser_download_url'])
        if status != 200:
            raise ConnectionError(f'download of GitHub asset {name} returned HTTP {status}')
        if sha256(data) != asset['digest'][7:]:
            raise ValueError(f'GitHub API digest mismatch for {name}')
        if (staged / name).is_symlink():
            raise ValueError('Staged release file must not be a symlink')
        (staged / name).write_bytes(data)
        digests[name] = sha256(data)
        print(f'Verified GitHub attachment: {name}', flush=True)
    listed = {}
    for line in (staged / 'SHA256SUMS.txt').read_text(encoding='utf-8-sig').splitlines():
        match = re.fullmatch(r'([a-f0-9]{64})  ([A-Za-z0-9._-]+)', line)
        if not match or match[2] in listed or digests.get(match[2]) != match[1]:
            raise ValueError(f'GitHub assets fail SHA256SUMS.txt at line: {line!r}')
        listed[match[2]] = match[1]
    if set(listed) != archive_names:
        raise ValueError('SHA256SUMS.txt must cover every package and source archive exactly once')
    provenance = json.loads((staged / 'release.json').read_text(encoding='utf-8'))
    if provenance.get('version') != tag.removeprefix('v') or provenance.get('commit') != commit:
        raise ValueError('release.json version/source differs from the published tag')
    if provenance.get('sha256') != listed:
        raise ValueError('release.json archive hashes differ from verified assets')
    desired = atomgit_status or ('pre' if prerelease else 'latest')
    if desired not in ('pre', 'latest'):
        raise ValueError('Unsupported AtomGit release_status')
    report = {'tag': tag, 'commit': commit, 'uploaded': [], 'already_synced': [], 'release_status': None}
    status, body = atomgit_api(atomgit_base, atomgit_owner, atomgit_repo, release_path(tag),
                               atomgit_token, expect=(200, 404))
    if status == 404:
        status, body = atomgit_api(atomgit_base, atomgit_owner, atomgit_repo, '/releases', atomgit_token,
                                   method='POST', expect=(200, 201),
                                   payload={'tag_name': tag, 'name': tag,
                                            'body': release.get('body') or '',
                                            'target_commitish': commit})
    remote = json.loads(body)
    mirrored = remote.get('target_commitish') or ''
    if mirrored != commit:
        raise ValueError(f'AtomGit release {tag} points at commit {mirrored}, expected {commit}')
    attached = [asset['name'] for asset in remote.get('assets', []) if asset.get('type') == 'attach']
    if len(set(attached)) != len(attached) or set(attached) - required:
        raise ValueError('Unexpected or duplicate AtomGit attachment')
    inventory = set(attached)
    download_path = f'{release_path(tag)}/attach_files'
    # Preflight ALL existing files before any upload. A later alphabetical
    # conflict must not follow an earlier PUT of a missing attachment.
    for name in sorted(digests):
        if name in inventory:
            print(f'Checking existing AtomGit attachment: {name}', flush=True)
            status, data = atomgit_api(atomgit_base, atomgit_owner, atomgit_repo,
                                       f'{download_path}/{urllib.parse.quote(name, safe="")}/download',
                                       atomgit_token)
            if sha256(data) != digests[name]:
                raise ValueError(f'AtomGit attachment {name} differs from the verified GitHub asset; '
                                 'published artifacts are immutable, so the sync stops here')
            report['already_synced'].append(name)
            print(f'Verified existing AtomGit attachment: {name}', flush=True)
    for name in sorted(digests):
        if name in inventory:
            continue
        print(f'Uploading missing AtomGit attachment: {name}', flush=True)
        status, body = atomgit_api(atomgit_base, atomgit_owner, atomgit_repo,
                                   f'{release_path(tag)}/upload_url', atomgit_token,
                                   query={'file_name': name})
        plan = json.loads(body)
        status, _ = http(plan['url'], method='PUT', data=(staged / name).read_bytes(),
                         headers=plan.get('headers') or {})
        if status >= 300:
            raise RuntimeError(f'AtomGit upload of {name} returned HTTP {status}')
        for attempt in range(3):
            status, data = atomgit_api(atomgit_base, atomgit_owner, atomgit_repo,
                                       f'{download_path}/{urllib.parse.quote(name, safe="")}/download',
                                       atomgit_token, expect=(200, 404))
            if status == 200:
                break
            time.sleep(BACKOFF_SECONDS[min(attempt, len(BACKOFF_SECONDS) - 1)])
        if status != 200 or sha256(data) != digests[name]:
            raise ValueError(f'uploaded AtomGit attachment {name} failed read-back verification; '
                             'leaving it in place for maintainer review')
        report['uploaded'].append(name)
        print(f'Verified uploaded AtomGit attachment: {name}', flush=True)
    _, body = atomgit_api(atomgit_base, atomgit_owner, atomgit_repo, release_path(tag), atomgit_token)
    remote = json.loads(body)
    if remote.get('release_status') != desired:
        atomgit_api(atomgit_base, atomgit_owner, atomgit_repo, release_path(tag), atomgit_token,
                    method='PATCH', payload={'name': remote.get('name') or tag,
                                             'body': remote.get('body') or '',
                                             'release_status': desired})
    _, body = atomgit_api(atomgit_base, atomgit_owner, atomgit_repo, release_path(tag), atomgit_token)
    final_status = json.loads(body).get('release_status')
    if final_status != desired:
        raise ValueError(f'release_status read-back returned {final_status!r}, expected {desired!r}')
    report['release_status'] = final_status
    (Path(workdir) / 'atomgit-sync-report.json').write_text(json.dumps(report, indent=2) + '\n')
    return report


def derive_preview_tag():
    request_path = ROOT / '.github' / 'release-request.json'
    if not request_path.exists():
        return None
    request = json.loads(request_path.read_text(encoding='utf-8'))
    version = ET.parse(ROOT / 'apps/windows/LocalRemoteDesktop.csproj').findtext('.//Version')
    if request.get('version') != version or request.get('prerelease') is not True:
        return None
    if not re.fullmatch(r'\d+\.\d+\.\d+-(alpha|beta|rc)\.[1-9]\d*', version or ''):
        return None
    return f'v{version}'


def main(argv=None):
    parser = argparse.ArgumentParser(description='Sync a verified GitHub release to AtomGit.')
    parser.add_argument('--work-dir', required=True)
    parser.add_argument('--tag')
    parser.add_argument('--commit')
    parser.add_argument('--stable', action='store_true',
                        help='expect a stable GitHub release; requires --tag')
    parser.add_argument('--atomgit-status', choices=('pre', 'latest'),
                        help='AtomGit homepage selection, independent of GitHub stability')
    parser.add_argument('--github-base', default=os.environ.get('GITHUB_API_BASE', 'https://api.github.com'))
    parser.add_argument('--github-repo', default=os.environ.get('GITHUB_REPOSITORY', 'kylezhang/local-remote-desktop'))
    parser.add_argument('--atomgit-base', default=os.environ.get('ATOMGIT_API_BASE', 'https://atomgit.com'))
    parser.add_argument('--atomgit-owner', default='zkxw2008')
    parser.add_argument('--atomgit-repo', default='local-remote-desktop')
    args = parser.parse_args(argv)
    ci = os.environ.get('GITHUB_ACTIONS') == 'true'
    atomgit_token = os.environ.get('ATOMGIT_RELEASE_TOKEN', '')
    if ci:
        if (os.environ.get('GITHUB_REPOSITORY') != 'kylezhang/local-remote-desktop'
                or os.environ.get('GITHUB_REF') != 'refs/heads/main'):
            print('Not the authorized repository and branch; skipping AtomGit sync.')
            return 0
        if not atomgit_token:
            print('ATOMGIT_RELEASE_TOKEN is not configured; skipping AtomGit sync.')
            return 0
    elif not atomgit_token:
        parser.error('ATOMGIT_RELEASE_TOKEN is required')
    if args.stable and not args.tag:
        parser.error('--stable requires an explicit --tag')
    github_token = os.environ.get('GH_TOKEN') or os.environ.get('GITHUB_TOKEN') or None
    tag, commit = args.tag, args.commit
    atomgit_status = args.atomgit_status
    if not tag:
        tag = derive_preview_tag()
        if not tag:
            print('No approved preview release request matches the client version; skipping AtomGit sync.')
            return 0
        if atomgit_status is None:
            request = json.loads((ROOT / '.github' / 'release-request.json').read_text(encoding='utf-8'))
            if request.get('version') == tag.removeprefix('v'):
                atomgit_status = request.get('atomgitStatus')
    if not commit:
        try:
            commit = fetch_tag_commit(args.github_base, args.github_repo, tag, github_token)
        except (LookupError, ValueError, RuntimeError, ConnectionError) as error:
            print(f'AtomGit sync stopped: {error}', file=sys.stderr)
            return 1
        checkout = os.environ.get('GITHUB_SHA', '')
        if checkout and checkout != commit:
            print(f'Mirroring the published {tag} at {commit}; this checkout is {checkout}.')
    if not re.fullmatch(r'[0-9a-f]{40}', commit or ''):
        print('A full 40-character commit SHA is required (--commit).', file=sys.stderr)
        return 1
    try:
        report = sync(tag, commit, not args.stable,
                      github_base=args.github_base, github_repo=args.github_repo, github_token=github_token,
                      atomgit_base=args.atomgit_base, atomgit_owner=args.atomgit_owner,
                      atomgit_repo=args.atomgit_repo, atomgit_token=atomgit_token,
                      workdir=Path(args.work_dir), atomgit_status=atomgit_status)
    except (LookupError, ValueError, RuntimeError, ConnectionError) as error:
        print(f'AtomGit sync stopped: {error}', file=sys.stderr)
        return 1
    print(f"AtomGit sync for {tag} at {commit}: uploaded {len(report['uploaded'])}, "
          f"already verified {len(report['already_synced'])}, release_status={report['release_status']}")
    for name in report['uploaded']:
        print(f'  uploaded {name}')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
