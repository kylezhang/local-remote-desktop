# LocalRemote 品牌图形

[English](README.md)

正式采用用户确认的蓝莓蓝紫方案：纯白屏幕轮廓、双向箭头和小指针。屏幕代表真实桌面，箭头表达双向连接，指针突出远程操作。图形保持平面，不含发光、浮雕、阴影或颗粒纹理。

![LocalRemote 蓝莓方案](identity-board.png)

## 配色

- 应用图标及主操作渐变：**#5265EC → #643AC2**。
- 浅色背景纯色标识、悬停按钮：**#4B50CE**。
- 按下按钮：**#4238AF**。
- 浅色背景品牌文字：**#282C67**；Windows 背景：**#F7F8FF**。
- Mac 深色模式品牌文字：**#B1B7FF**。

Windows 的文字选择、下拉选中高亮和自动重连勾选统一蓝色。Mac 原生控件使用蓝色主色，品牌文字适配明暗外观。主按钮正常、悬停和按下时保留白字对比度，禁用状态适度淡化。

## 文件

- `icon.svg`：已确认的可编辑矢量母版，含透明边距。
- `symbol.svg`：浅色背景使用的纯蓝标识，屏幕、箭头与指针共用母版。
- `icon-master.png`、`icon.png`：2048px 透明母版导出及 1024px 应用图标。
- `LocalRemote.ico`、`LocalRemote.icns`：Windows、macOS 图标。
- `identity-board.png`：图形、文字及应用图标展示。

采用协调后的 B：指针缩放 0.76、边框线宽 56、屏幕整体缩放 1.06、箭头缩放 1.08，指针略向左上做视觉居中。SVG 为正式制作源，各平台图标统一从它导出。此前车厘子红稿已被替代；最初探索使用内置 image_gen，本次沿用并调整既有矢量文件。

## 重新导出

安装 Node.js、sharp、Python 和 Pillow，在仓库根目录执行：

```sh
npm install --no-save --package-lock=false sharp
node scripts/render-brand.mjs
python scripts/generate-icons.py
```

保留透明边距。纯色标识导出同时处理屏幕和指针的填色/描边。展示板文字使用本机 Segoe UI/Arial；应用图标不含文字，不依赖这些字体。

两端顶部使用 PNG。Windows 嵌入 PNG/ICO，全屏时隐藏整个工具栏。Mac 打包 PNG/ICNS，并主动设置运行中的程序坞图标，覆盖登录代理直接启动。Windows 已构建和渲染；当前 Windows 环境无法实机验证 Mac 程序坞和原生主题。

图形表达产品愿景；当前功能仍为 Windows → Mac，双向控制及移动端属于规划。
