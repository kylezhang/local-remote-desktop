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

root = Path(__file__).resolve().parents[2]
version = ET.parse(root/'apps/windows/LocalRemote.csproj').findtext('./PropertyGroup/Version')
assert re.fullmatch(r'\d+\.\d+\.\d+(?:-(?:alpha|beta|rc)\.[1-9][0-9]*)?', version)
plist = plistlib.loads((root/'apps/macos/Info.plist').read_bytes())
assert plist['CFBundleShortVersionString'] == version.split('-')[0]
assert plist.get('LocalRemoteReleaseVersion', plist['CFBundleShortVersionString']) == version
assert plist['CFBundleIdentifier'] == 'com.remotemaclan.host'
assert plist['CFBundleIconFile'] == 'LocalRemote.icns'
assert plist['CFBundleExecutable'] == 'LocalRemote'
project = ET.parse(root/'apps/windows/LocalRemote.csproj')
assert project.findtext('./PropertyGroup/AssemblyName') == 'LocalRemote'
assert project.findtext('./PropertyGroup/RootNamespace') == 'LocalRemote'
assert 'name: "LocalRemote"' in (root/'apps/macos/Package.swift').read_text(encoding='utf-8')
assert f'LocalRemote · {version}' in (root/'apps/windows/MainWindow.xaml').read_text(encoding='utf-8')
assert f'LocalRemote {version}' in (root/'apps/macos/Sources/LocalRemote/App.swift').read_text(encoding='utf-8')
assert not (root/'experiments').exists()
print('PASS aligned public versions, legacy Mac identity and removed experiment')

for path in root.rglob('*.md'):
    if any(part in {'bin','obj','dist','.git','build'} for part in path.parts): continue
    text = path.read_text(encoding='utf-8')
    for target in re.findall(r'\[[^\]]*\]\(([^)]+)\)',text):
        if target.startswith(('https://','http://','#','mailto:')): continue
        destination = (path.parent/target.split('#')[0]).resolve()
        assert destination.is_relative_to(root) and destination.exists(), (path,target)
print('PASS documentation local links')

catalog = json.loads((root/'assets/localization/strings.json').read_text(encoding='utf-8'))
for source, english in catalog.items():
    assert sorted(re.findall(r'\{(\d+)\}', source)) == sorted(re.findall(r'\{(\d+)\}', english)), source
for key in re.findall(r"local:Translate Key='([^']+)'", (root/'apps/windows/MainWindow.xaml').read_text(encoding='utf-8')):
    assert key in catalog, key
assert 'strings.json' in (root/'apps/macos/build-app.sh').read_text(encoding='utf-8')
assert 'LocalRemote.strings.json' in (root/'apps/windows/LocalRemote.csproj').read_text(encoding='utf-8')
print('PASS shared language catalog placeholders, UI keys and bundle integration')

ico = (root/'assets/brand/LocalRemote.ico').read_bytes()
reserved, kind, count = struct.unpack('<HHH',ico[:6])
assert (reserved,kind)==(0,1) and count>=6
assert b'\x00\x00' in ico[6:6+16*count]
icns = (root/'assets/brand/LocalRemote.icns').read_bytes()
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
print('PASS LocalRemote source naming; legacy names restricted to migration code')
