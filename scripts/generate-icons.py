"""Export platform icon containers from the approved RGBA PNG master.

Requires Pillow. This performs size/format conversion only, preserving the
cherry-red artwork and its transparency. Run from any working directory.
"""
from pathlib import Path
from PIL import Image

brand = Path(__file__).resolve().parents[1] / 'assets' / 'brand'
with Image.open(brand / 'icon-master.png') as master:
    if master.mode != 'RGBA' or master.width != master.height or master.width < 1024:
        raise ValueError('Expected a square RGBA master of at least 1024 pixels')
    alpha_min, alpha_max = master.getchannel('A').getextrema()
    if alpha_min != 0 or alpha_max < 250:
        raise ValueError('Expected transparent margins and a solid icon interior')
    icon = master.resize((1024, 1024), Image.Resampling.LANCZOS)

icon.save(brand / 'icon.png')
icon.save(brand / 'LocalRemote.ico', sizes=[
    (16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)
])
icon.save(brand / 'LocalRemote.icns')
print('Generated PNG, multi-size ICO and ICNS from the approved PNG master')
