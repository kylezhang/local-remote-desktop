"""Static release checks for bilingual links, versions, icon formats and leaks.
Uses stdlib only; this is not a security audit or macOS native validation.
"""
from pathlib import Path
import plistlib
import re
import struct
import subprocess
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[2]
version = ET.parse(root/'windows-client/RemoteMacLAN.csproj').findtext('./PropertyGroup/Version')
assert re.fullmatch(r'\d+\.\d+\.\d+', version)
plist = plistlib.loads((root/'macos-host/Info.plist').read_bytes())
assert plist['CFBundleShortVersionString'] == version
assert plist['CFBundleIdentifier'] == 'com.remotemaclan.host'
assert plist['CFBundleIconFile'] == 'LocalRemote.icns'
assert f'LocalRemote · {version}' in (root/'windows-client/MainWindow.xaml').read_text(encoding='utf-8')
assert f'LocalRemote {version}' in (root/'macos-host/Sources/RemoteMacLAN/App.swift').read_text(encoding='utf-8')
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
for relative in filter(None,tracked):
    path=root/relative
    if not path.exists(): continue
    assert not any(part in {'bin','obj','.build','.build-native','dist','__pycache__'} for part in path.parts),relative
    assert path.suffix.lower() not in {'.dpapi','.pfx','.p12','.pem','.key','.zip','.exe','.dll','.log'},relative
    if path.suffix.lower() in {'.cs','.swift','.sh','.json','.ps1','.md'}:
        content=path.read_text(encoding='utf-8')
        assert not re.search(r'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----',content),relative
        assert not re.search(r'(?i)(?:access_token|api_key)\s*[:=]\s*[\"\'][^\"\']{12,}[\"\']',content),relative
print('PASS tracked-file hygiene and basic secret indicators (not a security audit)')
