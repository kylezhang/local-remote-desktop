"""Check direct Chinese UiText.text literals against the shared catalog.

Dynamic status/templates are covered by the Swift/.NET translation checks.
This is source coverage, not proof of every runtime language state.
"""
from pathlib import Path
import json
import re

root = Path(__file__).resolve().parents[2]
catalog = json.loads((root / 'assets/localization/strings.json').read_text(encoding='utf-8'))
missing = []
for source in (root / 'apps/macos/Sources/LocalRemoteDesktop').glob('*.swift'):
    for match in re.finditer(r'UiText\.text\("([^"\\]*)"\)', source.read_text(encoding='utf-8')):
        key = match[1]
        if re.search(r'[\u3400-\u9fff]', key) and key not in catalog:
            missing.append(f'{source.name}: {key}')
assert not missing, 'Untranslated direct UI literals:\n' + '\n'.join(missing)
print('PASS direct Mac UI translation keys exist in the shared English catalog')
