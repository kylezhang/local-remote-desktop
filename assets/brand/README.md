# LocalRemote identity

[简体中文](README.zh-CN.md)

The selected identity combines a universal screen outline with two opposing arrows. Its cherry-red gradient, edge highlights and soft glow express reciprocal connection across devices. The application icon uses the white symbol on a luminous cherry-red tile. Black and navy alternatives have been retired.

![LocalRemote cherry-red identity](identity-board.png)

## Assets

- `identity-board.png`: the selected logo, wordmark and app icon presentation.
- `icon-master.png`: the approved square RGBA raster master, including transparent margins and glow.
- `icon.png`: 1024px app icon used by the repository README.
- `LocalRemote.ico`: Windows sizes from 16px through 256px.
- `LocalRemote.icns`: macOS icon representations.

The artwork was developed with user direction and the built-in image_gen tool. The PNG master is the source of truth; it is not editable vector artwork. The obsolete desktop/mobile SVG and its limited renderer were removed.

The current artwork softens the original cherry red into a muted palette around **#B9677A**, with gentler shadows, pale highlights and reduced glow. Interface actions use **#A4546B** to retain text contrast. This is a screen-color interpretation of a user-supplied paint swatch, not an official paint specification. The icon gradient is embedded in the approved artwork.

## Regeneration

With Python and Pillow installed, run from the repository root:

```sh
python scripts/generate-icons.py
```

The script performs size and format conversion only. Preserve the master alpha channel; do not flatten transparent margins onto a black or white background. Windows and macOS builds already consume the generated ICO/ICNS files.

Both application headers display the selected artwork. Windows embeds the PNG as a WPF resource, keeps the whole header inside the full-screen toolbar, and uses a cherry-red connection action. macOS packages PNG and ICNS, explicitly sets the running Dock icon from the PNG (ICNS fallback), and uses the same image in its header.

Brand imagery expresses the product vision. The current application supports Windows → Mac; reverse control and mobile support remain planned.
