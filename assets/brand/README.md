# LocalRemote identity

[简体中文](README.zh-CN.md)

A universal screen outline and two opposing arrows express reciprocal desktop control across devices. The mark is flat and solid; only the application tile has a subtle cherry-red gradient. It has no glow, bevel, shadow or grain texture.

![LocalRemote identity](identity-board.png)

## Assets

- `icon.svg`: editable application icon master, with transparent margins.
- `symbol.svg`: solid cherry-red symbol for light backgrounds, derived from the same geometry.
- `icon-master.png`: 2048px RGBA export of the vector master.
- `icon.png`: 1024px app/header icon.
- `LocalRemote.ico`: Windows sizes from 16px through 256px.
- `LocalRemote.icns`: macOS icon representations.
- `identity-board.png`: symbol, wordmark and application icon presentation.

The tile gradient runs from **#C77A8D** to **#A45A70**, brightened while retaining the muted cherry-red direction. The standalone symbol and interface actions use **#A4546B**; wordmark text uses **#683247**. This palette interprets a user-supplied paint swatch for screens; it is not an official paint specification.

The design was explored with the built-in image_gen tool, then reconstructed as editable SVG geometry. The SVG is now the production source of truth. Generated raster exploration is reference material, not the platform icon master. The earlier luminous artwork has been superseded.

## Regeneration

Install Node.js, sharp, Python and Pillow. From the repository root:

```sh
npm install --no-save --package-lock=false sharp
node scripts/render-brand.mjs
python scripts/generate-icons.py
```

Rendering uses sharp; container conversion uses Pillow. Keep transparent margins intact. The screen outline, arrows and tile contain no bitmap data or visual-effect filters. The presentation wordmark uses the available Segoe UI/Arial font; platform icons contain no text and do not depend on font availability.

Both app headers use the exported PNG. Windows embeds PNG/ICO resources, keeps the entire header inside the full-screen toolbar, and uses a cherry-red connection action. macOS packages PNG/ICNS and explicitly sets the running Dock icon (including direct LaunchAgent starts).

The identity expresses the product vision. Current functionality is Windows → Mac; reverse control and mobile support remain planned.
