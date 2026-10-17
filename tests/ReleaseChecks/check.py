"""Static release checks for bilingual links, versions, icon formats and leaks.
Uses stdlib only; this is not a security audit or macOS native validation.
"""
from pathlib import Path
import json
import plistlib
import re
import struct
import subprocess
import xml.etree.ElementTree as ET
from urllib.parse import unquote, urlsplit

root = Path(__file__).resolve().parents[2]
license_text = (root/'LICENSE').read_text(encoding='utf-8')
assert 'GNU GENERAL PUBLIC LICENSE' in license_text and 'Version 3, 29 June 2007' in license_text
assert 'END OF TERMS AND CONDITIONS' in license_text and len(license_text) > 30000
for document in ('README.md', 'README.zh-CN.md', 'NOTICE.md', 'CONTRIBUTING.md'):
    assert 'GPL-3.0-or-later' in (root/document).read_text(encoding='utf-8'), document
assert ET.parse(root/'apps/windows/LocalRemoteDesktop.csproj').findtext('./PropertyGroup/PackageLicenseExpression') == 'GPL-3.0-or-later'
print('PASS GPL-3.0-or-later license and bilingual project declarations')
version = ET.parse(root/'apps/windows/LocalRemoteDesktop.csproj').findtext('./PropertyGroup/Version')
assert re.fullmatch(r'\d+\.\d+\.\d+(?:-(?:alpha|beta|rc)\.[1-9][0-9]*)?', version)
plist = plistlib.loads((root/'apps/macos/Info.plist').read_bytes())
assert plist['CFBundleShortVersionString'] == version.split('-')[0]
assert plist.get('LocalRemoteDesktopReleaseVersion', plist['CFBundleShortVersionString']) == version
assert plist['CFBundleIdentifier'] == 'com.remotemaclan.host'
assert plist['CFBundleIconFile'] == 'LocalRemoteDesktop.icns'
assert plist['CFBundleExecutable'] == 'LocalRemoteDesktop'
assert plist['CFBundleName'] == plist['CFBundleDisplayName'] == 'Local Remote Desktop'
project = ET.parse(root/'apps/windows/LocalRemoteDesktop.csproj')
assert project.findtext('./PropertyGroup/AssemblyName') == 'LocalRemoteDesktop'
assert project.findtext('./PropertyGroup/RootNamespace') == 'LocalRemoteDesktop'
assert project.findtext('./PropertyGroup/Product') == 'Local Remote Desktop'
assert not (root/'apps/windows/LocalRemote.csproj').exists()
assert not (root/'apps/macos/Sources/LocalRemote').exists()
assert ',"LocalRemote"' in (root/'apps/windows/SecretStore.cs').read_text(encoding='utf-8')
assert ',"LocalRemote","language.txt"' in (root/'apps/windows/UiText.cs').read_text(encoding='utf-8')
assert '"LocalRemote/ReceivedFiles"' in (root/'apps/macos/Sources/LocalRemoteDesktop/MacFiles.swift').read_text(encoding='utf-8')
assert 'name: "LocalRemoteDesktop"' in (root/'apps/macos/Package.swift').read_text(encoding='utf-8')
assert f'Local Remote Desktop · {version}' in (root/'apps/windows/MainWindow.xaml').read_text(encoding='utf-8')
assert f'Local Remote Desktop {version}' in (root/'apps/macos/Sources/LocalRemoteDesktop/App.swift').read_text(encoding='utf-8')
assert not (root/'experiments').exists()
print('PASS aligned public versions, legacy Mac identity and removed experiment')

for path in root.rglob('*.md'):
    if any(part in {'bin','obj','dist','.git','build'} for part in path.parts): continue
    text = path.read_text(encoding='utf-8')
    for target in re.findall(r'\[[^\]]*\]\(([^)]+)\)',text):
        if target.startswith(('https://','http://','#','mailto:')): continue
        local = urlsplit(target)
        assert not local.scheme and not local.netloc, (path,target)
        destination = (path.parent/unquote(local.path)).resolve()
        assert destination.is_relative_to(root) and destination.exists(), (path,target)
print('PASS documentation local links')

catalog = json.loads((root/'assets/localization/strings.json').read_text(encoding='utf-8'))
for source, english in catalog.items():
    assert sorted(re.findall(r'\{(\d+)\}', source)) == sorted(re.findall(r'\{(\d+)\}', english)), source
for xaml in (root/'apps/windows').rglob('*.xaml'):
    if any(p in {'bin','obj'} for p in xaml.parts): continue
    for key in re.findall(r"local:Translate Key='([^']+)'", xaml.read_text(encoding='utf-8')):
        assert key in catalog, (xaml,key)
assert 'strings.json' in (root/'apps/macos/build-app.sh').read_text(encoding='utf-8')
assert 'LocalRemoteDesktop.strings.json' in (root/'apps/windows/LocalRemoteDesktop.csproj').read_text(encoding='utf-8')
print('PASS shared language catalog placeholders, UI keys and bundle integration')

ico = (root/'assets/brand/LocalRemoteDesktop.ico').read_bytes()
reserved, kind, count = struct.unpack('<HHH',ico[:6])
assert (reserved,kind)==(0,1) and count>=6
assert b'\x00\x00' in ico[6:6+16*count]
icns = (root/'assets/brand/LocalRemoteDesktop.icns').read_bytes()
assert icns[:4]==b'icns' and struct.unpack('>I',icns[4:8])[0]==len(icns)
master = (root/'assets/brand/icon-master.png').read_bytes()
assert master.startswith(b'\x89PNG\r\n\x1a\n')
width, height, depth, color_type = struct.unpack('>IIBB', master[16:26])
assert width == height and width >= 1024 and depth == 8 and color_type == 6
assert (root/'assets/brand/icon.png').read_bytes().startswith(b'\x89PNG\r\n\x1a\n')
print('PASS square RGBA master/PNG/multi-size ICO/ICNS containers')

tracked = subprocess.check_output(['git','ls-files','-z'],cwd=root).decode().split('\0')
assert not any(p.startswith(('macos-host/', 'windows-client/')) for p in tracked if p and (root/p).exists()), 'Old source layout remains tracked'
legacy_migration_files = {
    'apps/macos/install-app.sh', 'apps/macos/diagnose-permissions.sh',
    'apps/windows/SecretStore.cs', 'tests/InstallerChecks/check.py',
    'tests/ProtocolChecks/Program.cs',
    'tests/NamingChecks/Program.cs',
}
for relative in filter(None,tracked):
    path=root/relative
    if not path.exists(): continue
    assert not any(part in {'bin','obj','.build','.build-native','dist','__pycache__'} for part in path.parts),relative
    assert path.suffix.lower() not in {'.dpapi','.pfx','.p12','.pem','.key','.zip','.exe','.dll','.log'},relative
    if path.suffix.lower() in {'.cs','.swift','.sh','.json','.ps1','.md'}:
        content=path.read_text(encoding='utf-8')
        assert not re.search(r'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----',content),relative
        assert not re.search(r'(?i)(?:access_token|api_key)\s*[:=]\s*[\"\'][^\"\']{12,}[\"\']',content),relative
    if path.suffix.lower() in {'.cs','.xaml','.csproj','.swift','.ps1','.sh','.py'}:
        content = path.read_text(encoding='utf-8')
        assert 'RemoteMacLAN' not in content or relative in legacy_migration_files or relative == 'tests/ReleaseChecks/check.py', ('Unexpected legacy source name', relative)
print('PASS tracked-file hygiene and basic secret indicators (not a security audit)')
print('PASS LocalRemoteDesktop source naming; legacy names restricted to migration code')
