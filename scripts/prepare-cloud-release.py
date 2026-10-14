"""Collect exact native CI assets; reject mismatched provenance or checksums."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import plistlib
import re
import shutil
import zipfile

ROOT = Path(__file__).resolve().parents[1]


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def verify(directory, names, version, commit, platform):
    metadata = json.loads((directory / 'ci.json').read_text(encoding='utf-8-sig'))
    if (metadata.get('version'), metadata.get('commit'), metadata.get('platform')) != (version, commit, platform):
        raise ValueError('CI provenance mismatch')
    hashes = {}
    for line in (directory / 'SHA256SUMS.txt').read_text(encoding='utf-8-sig').splitlines():
        match = re.fullmatch(r'([a-f0-9]{64})  ([A-Za-z0-9._-]+)', line)
        if not match or match[2] in hashes:
            raise ValueError('Invalid/duplicate checksum entry')
        hashes[match[2]] = match[1]
    if set(hashes) != set(names):
        raise ValueError('Unexpected or missing CI asset')
    for name in names:
        path = directory / name
        if path.is_symlink() or not path.is_file() or digest(path) != hashes[name]:
            raise ValueError('CI asset checksum mismatch')
    return metadata


def prepare(windows, macos, output, version, commit, run):
    if not re.fullmatch(r'\d+\.\d+\.\d+-(alpha|beta|rc)\.[1-9]\d*', version):
        raise ValueError('This pipeline only publishes explicit pre-releases')
    if not re.fullmatch(r'[a-f0-9]{40}', commit) or not str(run).isdigit():
        raise ValueError('Full commit and numeric CI run required')
    base = f'LocalRemoteDesktop-v{version}'
    windows_name = base + '-windows-x64.zip'
    mac_base = base + '-macos-arm64-preview'
    names = [windows_name, mac_base + '.zip', mac_base + '.dmg']
    windows_metadata = verify(windows, names[:1], version, commit, 'windows-x64')
    mac_metadata = verify(macos, names[1:], version, commit, 'macos-arm64')
    required = {'ProtocolChecks', 'FileChecks', 'ClipboardChecks', 'LocalizationChecks', 'DeviceNameChecks', 'DiscoveryChecks', 'WindowsHostChecks', 'ClientChecks'}
    if not required.issubset(set(windows_metadata.get('passed', []))) or mac_metadata.get('signing') != 'ad-hoc' or mac_metadata.get('notarized') is not False:
        raise ValueError('Missing checks or incorrect signing disclosure')
    with zipfile.ZipFile(macos / names[1]) as archive:
        app = mac_base + '/Local Remote Desktop.app/Contents/'
        info = plistlib.loads(archive.read(app + 'Info.plist'))
        revision = archive.read(app + 'Resources/source-revision.txt').decode().strip()
        if info.get('LocalRemoteDesktopReleaseVersion') != version or revision != commit:
            raise ValueError('Packaged Mac identity/source mismatch')
        source = archive.read(mac_base + '/source.zip')
    spec = importlib.util.spec_from_file_location('checks', ROOT / 'scripts/check-macos-package.py')
    checks = importlib.util.module_from_spec(spec); spec.loader.exec_module(checks)
    checks.check_source_archive(source, commit, (ROOT / 'LICENSE').read_bytes())
    if output.exists():
        raise ValueError('Refusing to overwrite release output')
    output.mkdir(parents=True)
    for name in names:
        shutil.copyfile((windows if name == windows_name else macos) / name, output / name)
    source_name = base + '-source.zip'
    (output / source_name).write_bytes(source)
    hashes = {name: digest(output / name) for name in names + [source_name]}
    (output / 'SHA256SUMS.txt').write_text(''.join(f'{value}  {name}\n' for name, value in hashes.items()), encoding='ascii')
    (output / 'release.json').write_text(json.dumps({
        'version': version, 'commit': commit, 'run': int(run), 'prerelease': True,
        'nativeAcceptance': 'deferred-by-maintainer', 'macSigning': 'ad-hoc',
        'macNotarized': False, 'sha256': hashes,
        'ci': f'https://github.com/kylezhang/local-remote-desktop/actions/runs/{run}',
        'notRun': ['Interactive TCC/Keychain and upgrade acceptance', 'Physical two-device input/clipboard/files', 'Sustained hardware performance']
    }, indent=2) + '\n', encoding='utf-8')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for key in ('windows', 'macos', 'output', 'version', 'commit', 'run'):
        parser.add_argument('--' + key, required=True)
    args = parser.parse_args()
    prepare(Path(args.windows), Path(args.macos), Path(args.output), args.version, args.commit, args.run)
    print('PASS exact CI assets, source and pre-release provenance')
