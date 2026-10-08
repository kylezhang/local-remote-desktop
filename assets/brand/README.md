# LocalRemote icon

Original vector design: two displays and two local connection arrows, with pale screen outlines and mint links on a dark blue rounded tile. The paired arrows express the long-term two-way product vision; v0.1.0 currently supports Windows→Mac only.

`icon.svg` is editable source. `icon.png` is the 1024px preview. `LocalRemote.ico` contains Windows sizes from 16px through 256px; `LocalRemote.icns` contains Mac icon representations. Run `python scripts/generate-icons.py` with Pillow installed to regenerate them. The generator reads the SVG's restricted rectangle/orthogonal-path vocabulary; adding other SVG primitives requires extending that renderer.

中文：原创图标以两块屏幕和连接箭头表达局域网及长期双向愿景，不代表当前已经支持双向。SVG 为可编辑源，PNG 是预览，ICO/ICNS 接入两端。使用 Pillow 运行生成脚本；扩展 SVG 图形类型时需要同步扩展渲染器。
