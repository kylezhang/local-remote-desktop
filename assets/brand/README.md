# LocalRemote identity

[简体中文](README.zh-CN.md)

The approved blueberry identity combines a white screen outline, two opposing arrows and a compact pointer. The screen represents a live desktop, the arrows express reciprocal connection, and the pointer makes remote interaction explicit. The geometry is flat: no glow, bevel, shadow or grain texture.

![LocalRemote blueberry identity](identity-board.png)

## Palette

- Application tile and primary action: **#5265EC → #643AC2**.
- Solid symbol and hover action: **#4B50CE**.
- Pressed action: **#4238AF**.
- Light-surface brand text: **#282C67**; Windows surface: **#F7F8FF**.
- Mac dark-appearance brand text: **#B1B7FF**.

Windows uses the same blue for text selection, dropdown highlights and checked reconnect state. macOS uses the blue accent for native controls and adapts its brand text to light/dark appearance. White primary-action labels retain contrast in normal, hover and pressed states. Disabled controls remain visually muted.

## Assets

- `icon.svg`: approved editable icon master, with transparent margins.
- `symbol.svg`: same screen/arrows/pointer in solid blue for light backgrounds.
- `icon-master.png`: 2048px RGBA export.
- `icon.png`: 1024px app/header icon.
- `LocalRemote.ico`: Windows sizes from 16px through 256px.
- `LocalRemote.icns`: macOS representations.
- `identity-board.png`: symbol, wordmark and app icon presentation.

The pointer and frame use the refined B approved by the user: pointer scale 0.76, screen stroke 56, overall screen scale 1.06 and arrow scale 1.08. The pointer is optically centered slightly upward/left. The SVG is the production source of truth; all platform icons derive from it. Earlier cherry-red explorations have been superseded. Initial brand exploration used built-in image_gen; this refinement edits the established vector artwork.

## Regeneration

Install Node.js, sharp, Python and Pillow. From the repository root:

```sh
npm install --no-save --package-lock=false sharp
node scripts/render-brand.mjs
python scripts/generate-icons.py
```

Preserve transparent margins. Symbol export recolors both the screen and pointer, including its stroke. The presentation wordmark uses Segoe UI/Arial when available; app icons contain no text and do not require these fonts.

Both app headers use the PNG. Windows embeds PNG/ICO and hides the entire toolbar in full screen. macOS packages PNG/ICNS and sets the running Dock icon, including direct LaunchAgent starts. Windows is built and rendered; actual Mac Dock and native theme behavior remain unverified in this Windows environment.

The graphic expresses the product vision. Current functionality is Windows → Mac; reverse control and mobile support remain planned.
