"""Portable package checks. These do not verify Apple signatures or notarization."""
import argparse
import hashlib
import io
import json
from pathlib import Path, PurePosixPath
import plistlib
import re
import struct
import zipfile


def check_source_archive(content, commit, license_text):
    prefix = f'LocalRemoteDesktop-source-{commit}/'
    required = {
        'LICENSE', 'NOTICE.md', 'README.md', 'README.zh-CN.md',
        'apps/macos/Package.swift', 'apps/macos/build-app.sh',
        'apps/windows/LocalRemoteDesktop.csproj', 'apps/windows/BUILD.md',
    }
    with zipfile.ZipFile(io.BytesIO(content)) as source:
        if source.comment != commit.encode('ascii'):
            raise ValueError('Corresponding source commit mismatch')
        if sum(i.file_size for i in source.infolist()) > 512 * 1024 * 1024:
            raise ValueError('Source snapshot exceeds size limit')
        seen = set()
        for entry in source.infolist():
            name = entry.filename
            if (name in seen or not name.startswith(prefix) or '\\' in name
                    or '..' in PurePosixPath(name).parts or ':' in name
                    or ((entry.external_attr >> 16) & 0o170000) == 0o120000):
                raise ValueError('Unsafe or duplicate source entry')
            seen.add(name)
        if not {prefix + p for p in required}.issubset(seen):
            raise ValueError('Corresponding source or build instructions missing')
        if source.read(prefix + 'LICENSE') != license_text:
            raise ValueError('Source and binary license mismatch')


def check_files(files, version):
    required = {
        'Contents/Info.plist', 'Contents/MacOS/LocalRemoteDesktop',
        'Contents/Resources/LocalRemoteDesktop.icns', 'Contents/Resources/LocalRemoteDesktop.png',
        'Contents/Resources/signing-mode.txt', 'Contents/_CodeSignature/CodeResources',
        'Contents/Resources/strings.json',
    }
    if not required.issubset(files):
        raise ValueError('Missing required application files')
    entry_files = {'Contents/Resources/initiate-connection.png', 'Contents/Resources/accept-connection.png', 'Contents/Resources/source-revision.txt'}
    # Stapler may add a ticket at Contents/CodeResources. No arbitrary resources
    # or credentials from a previous local build are permitted in the bundle.
    if set(files) - required - entry_files - {'Contents/CodeResources'}:
        raise ValueError('Unexpected application files')
    info = plistlib.loads(files['Contents/Info.plist'])
    catalog = json.loads(files['Contents/Resources/strings.json'])
    if (not isinstance(catalog, dict) or catalog.get('开始共享') not in {'Start sharing', 'Start accepting connections'}
            or catalog.get('目标IP') != 'Target IP'
            or any(not isinstance(k, str) or not isinstance(v, str) for k, v in catalog.items())):
        raise ValueError('Invalid shared language catalog')
    if (info.get('CFBundleIdentifier') != 'com.remotemaclan.host'
            or info.get('CFBundleShortVersionString') != version
            or info.get('LocalRemoteDesktopReleaseVersion', info.get('CFBundleShortVersionString')) != version
            or info.get('CFBundleExecutable') != 'LocalRemoteDesktop'
            or info.get('CFBundleIconFile') != 'LocalRemoteDesktop.icns'
            or not re.fullmatch(r'[1-9][0-9]*', str(info.get('CFBundleVersion', '')))):
        raise ValueError('Incorrect application identity, version or icon')
    if int(info['CFBundleVersion']) >= 24:
        if not entry_files.issubset(files):
            raise ValueError('Missing entry art or source identity')
        if not re.fullmatch(rb'[0-9a-f]{40}', files['Contents/Resources/source-revision.txt'].strip()):
            raise ValueError('Release source identity is unknown or modified')
        for path in sorted(entry_files - {'Contents/Resources/source-revision.txt'}):
            data = files[path]
            if not data.startswith(b'\x89PNG\r\n\x1a\n') or len(data) < 24 or struct.unpack('>II',data[16:24]) != (960,744):
                raise ValueError('Invalid entry illustration')
    executable = files['Contents/MacOS/LocalRemoteDesktop']
    if len(executable) < 32 or struct.unpack('<II', executable[:8]) != (0xFEEDFACF, 0x0100000C):
        raise ValueError('Expected a thin Apple Silicon Mach-O executable')
    png = files['Contents/Resources/LocalRemoteDesktop.png']
    icns = files['Contents/Resources/LocalRemoteDesktop.icns']
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
    folder = f'LocalRemoteDesktop-v{version}-macos-arm64'
    app_prefix = folder + '/Local Remote Desktop.app/'
    required_docs = {folder + '/' + name for name in (
        'LICENSE', 'NOTICE.md', 'SOURCE.txt', 'source.zip', 'START-HERE.txt', 'release.json')}
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
    license_text = files[folder + '/LICENSE']
    if (b'GNU GENERAL PUBLIC LICENSE' not in license_text
            or b'Version 3, 29 June 2007' not in license_text):
        raise ValueError('GPL version 3 license missing')
    source_notice = files[folder + '/SOURCE.txt'].decode('utf-8-sig')
    if commit not in source_notice or 'GPL-3.0-or-later' not in source_notice:
        raise ValueError('Corresponding source notice mismatch')
    check_source_archive(files[folder + '/source.zip'], commit, license_text)
    app_files = {n[len(app_prefix):]: b for n, b in files.items() if n.startswith(app_prefix)}
    check_files(app_files, version)
    if 'Contents/Resources/source-revision.txt' in app_files and app_files['Contents/Resources/source-revision.txt'].strip() != commit.encode('ascii'):
        raise ValueError('Application and source archive commits differ')
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
