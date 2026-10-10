"""Portable package checks. These do not verify Apple signatures or notarization."""
import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import plistlib
import re
import struct
import zipfile


def check_files(files, version):
    required = {
        'Contents/Info.plist', 'Contents/MacOS/LocalRemote',
        'Contents/Resources/LocalRemote.icns', 'Contents/Resources/LocalRemote.png',
        'Contents/Resources/signing-mode.txt', 'Contents/_CodeSignature/CodeResources',
        'Contents/Resources/strings.json',
    }
    if not required.issubset(files):
        raise ValueError('Missing required application files')
    # Stapler may add a ticket at Contents/CodeResources. No arbitrary resources
    # or credentials from a previous local build are permitted in the bundle.
    if set(files) - required - {'Contents/CodeResources'}:
        raise ValueError('Unexpected application files')
    info = plistlib.loads(files['Contents/Info.plist'])
    catalog = json.loads(files['Contents/Resources/strings.json'])
    if (not isinstance(catalog, dict) or catalog.get('开始共享') != 'Start sharing'
            or catalog.get('目标IP') != 'Target IP'
            or any(not isinstance(k, str) or not isinstance(v, str) for k, v in catalog.items())):
        raise ValueError('Invalid shared language catalog')
    if (info.get('CFBundleIdentifier') != 'com.remotemaclan.host'
            or info.get('CFBundleShortVersionString') != version
            or info.get('LocalRemoteReleaseVersion', info.get('CFBundleShortVersionString')) != version
            or info.get('CFBundleExecutable') != 'LocalRemote'
            or info.get('CFBundleIconFile') != 'LocalRemote.icns'
            or not re.fullmatch(r'[1-9][0-9]*', str(info.get('CFBundleVersion', '')))):
        raise ValueError('Incorrect application identity, version or icon')
    executable = files['Contents/MacOS/LocalRemote']
    if len(executable) < 32 or struct.unpack('<II', executable[:8]) != (0xFEEDFACF, 0x0100000C):
        raise ValueError('Expected a thin Apple Silicon Mach-O executable')
    png = files['Contents/Resources/LocalRemote.png']
    icns = files['Contents/Resources/LocalRemote.icns']
    if not png.startswith(b'\x89PNG\r\n\x1a\n') or len(png) < 24:
        raise ValueError('Invalid PNG icon')
    if len(icns) < 8 or icns[:4] != b'icns' or struct.unpack('>I', icns[4:8])[0] != len(icns):
        raise ValueError('Invalid ICNS container')
    if files['Contents/Resources/signing-mode.txt'].strip() != b'certificate':
        raise ValueError('Development signing mode is not distributable')
    return info


def check_app(app, version):
    app = Path(app)
    if app.is_symlink() or not app.is_dir():
        raise ValueError('Expected an application directory, not a symlink')
    files = {}
    for path in app.rglob('*'):
        if path.is_symlink():
            raise ValueError('Symlinks are not permitted in this single-executable bundle')
        if path.is_file():
            files[path.relative_to(app).as_posix()] = path.read_bytes()
        elif not path.is_dir():
            raise ValueError('Unsupported filesystem entry')
    return check_files(files, version)


def check_archive(archive, version, commit):
    if not re.fullmatch(r'[0-9a-f]{40}', commit or ''):
        raise ValueError('A full source commit is required')
    folder = f'LocalRemote-v{version}-macos-arm64'
    app_prefix = folder + '/LocalRemote.app/'
    required_docs = {folder + '/' + name for name in ('NOTICE.md', 'START-HERE.txt', 'release.json')}
    files = {}
    with zipfile.ZipFile(archive) as package:
        if sum(i.file_size for i in package.infolist()) > 512 * 1024 * 1024:
            raise ValueError('Package exceeds size limit')
        seen = set()
        for entry in package.infolist():
            name = entry.filename
            if (name in seen or '\\' in name or name.startswith('/')
                    or '..' in PurePosixPath(name).parts or ':' in name
                    or ((entry.external_attr >> 16) & 0o170000) == 0o120000):
                raise ValueError('Unsafe or duplicate archive entry')
            seen.add(name)
            # ditto adds extended attributes and the stapled ticket in __MACOSX.
            # Allow only AppleDouble headers referring to an actual archive entry.
            if name.startswith('__MACOSX/'):
                if not entry.is_dir():
                    original = PurePosixPath(name[len('__MACOSX/'):])
                    if not original.name.startswith('._'):
                        raise ValueError('Unexpected extended attribute entry')
                    original = str(original.with_name(original.name[2:]))
                    if original not in package.namelist() and original + '/' not in package.namelist():
                        raise ValueError('Orphan extended attribute entry')
                    if not package.read(entry).startswith(b'\x00\x05\x16\x07'):
                        raise ValueError('Invalid AppleDouble header')
                continue
            if entry.is_dir():
                if not (name == folder + '/' or name.startswith(app_prefix)):
                    raise ValueError('Unexpected archive directory')
                continue
            if not (name.startswith(app_prefix) or name in required_docs):
                raise ValueError('Unexpected archive file')
            files[name] = package.read(entry)
    if not required_docs.issubset(files):
        raise ValueError('Missing release documentation')
    check_files({n[len(app_prefix):]: b for n, b in files.items() if n.startswith(app_prefix)}, version)
    manifest = json.loads(files[folder + '/release.json'])
    if manifest.get('version') != version or manifest.get('commit') != commit:
        raise ValueError('Release provenance mismatch')
    actual = {n[len(folder)+1:]: hashlib.sha256(b).hexdigest()
              for n, b in files.items() if n != folder + '/release.json'}
    if manifest.get('sha256') != actual:
        raise ValueError('Release file hashes mismatch')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    target = parser.add_mutually_exclusive_group(required=True)
    target.add_argument('--app')
    target.add_argument('--archive')
    parser.add_argument('--version', required=True)
    parser.add_argument('--commit')
    parser.add_argument('--manifest-output')
    args = parser.parse_args()
    if not re.fullmatch(r'\d+\.\d+\.\d+', args.version):
        parser.error('Expected a semantic release version')
    if args.app:
        info = check_app(args.app, args.version)
        if args.manifest_output:
            if not re.fullmatch(r'[0-9a-f]{40}', args.commit or ''):
                parser.error('Manifest requires a full commit hash')
            payload = Path(args.app).parent
            files = {p.relative_to(payload).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
                     for p in payload.rglob('*') if p.is_file()}
            Path(args.manifest_output).write_text(json.dumps({
                'version': args.version, 'build': info['CFBundleVersion'],
                'commit': args.commit, 'platform': 'macos-arm64', 'sha256': files,
            }, indent=2) + '\n', encoding='utf-8')
    else:
        check_archive(args.archive, args.version, args.commit)
    print('PASS package structure, identity, icons and provenance (native signing checked separately)')


if __name__ == '__main__':
    main()
