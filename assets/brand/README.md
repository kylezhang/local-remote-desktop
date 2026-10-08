# LocalRemote icon

Original vector design: a desktop display, a portrait mobile device and an input pointer on a dark blue rounded tile. Pale outlines identify the desktop, mint identifies the mobile device and remote interaction. The mixed device shapes express the cross-device product vision without relying on platform logos or a desktop-only connection diagram. Mobile support remains planned; the current application supports Windows→Mac.

`icon.svg` is editable source. `icon.png` is the 1024px preview. `LocalRemote.ico` contains Windows sizes from 16px through 256px; `LocalRemote.icns` contains Mac icon representations. Run `python scripts/generate-icons.py` with Pillow installed to regenerate them. The generator reads a restricted rectangle/polygon/orthogonal-path vocabulary; adding other SVG primitives requires extending that renderer.

中文：原创图标结合桌面屏幕、移动设备与操作指针，表达跨设备远控，避免只像 PC 互连。手机是产品愿景，当前尚未提供移动端。SVG 为源，PNG 为预览，ICO/ICNS 接入两端；使用 Pillow 生成，扩展 SVG 类型时同步扩展渲染器。
