# LocalRemote 品牌图形

[English](README.md)

正式方案为**车厘子红渐变发光版**：通用屏幕外框与双向箭头融成一个标识，应用图标使用红色渐变底和白色标识。黑色、深蓝备选已停用。

![LocalRemote 车厘子红方案](identity-board.png)

## 文件

- `identity-board.png`：正式 Logo、文字标识及应用图标展示。
- `icon-master.png`：带透明边距和光晕的正方形 RGBA 原图。
- `icon.png`：README 使用的 1024px 图标。
- `LocalRemote.ico`、`LocalRemote.icns`：Windows、macOS 图标。

图形根据用户意见使用内置 image_gen 制作，PNG 原图为当前制作源，并非可编辑矢量图。旧桌面/手机 SVG 及其简易渲染器已移除。

目前将原车厘子红柔化为 **#B9677A** 附近的低饱和渐变，减弱阴影对比、高光和光晕。操作按钮采用 **#A4546B**，保持文字对比度。这是用户提供车漆色块的屏幕近似色，并非官方车漆色号。

安装 Python 和 Pillow 后，在仓库根目录执行 `python scripts/generate-icons.py` 重新导出。脚本只做尺寸和格式转换，保留透明通道。两端构建已经接入 ICO/ICNS。

两端应用顶部已使用正式图标。Windows 将 PNG 嵌入 WPF 资源，全屏时整个品牌工具栏隐藏。Mac 同时打包 PNG 和 ICNS，主动设置运行中的程序坞图标，并将同一图形用于应用顶部。

当前功能仍为 Windows → Mac；双向控制和移动端属于规划。
