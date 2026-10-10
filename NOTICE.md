# Copyright, license and distribution

Copyright (C) 2026 zkxw2008 and contributors.

Local Remote Desktop's original source code, documentation and brand assets are licensed under the GNU General Public License, version 3 or any later version (`SPDX-License-Identifier: GPL-3.0-or-later`), unless a file explicitly states otherwise. The full license is in [LICENSE](LICENSE). This program is distributed without any warranty, including the implied warranties of merchantability or fitness for a particular purpose. The project's name and logo do not imply endorsement of modified distributions.

Future binary packages include LICENSE, this notice, SOURCE.txt identifying the exact source commit and a source.zip snapshot containing the project's corresponding source and build scripts. A private repository link alone is not the source-delivery mechanism. Existing release artifacts and repository visibility are not changed by adopting this license.

Windows self-contained packages include Microsoft .NET 8 and Windows Desktop runtime components. The release packager copies the runtime licenses and third-party notices from the exact installed .NET runtime packs/SDK used for that build. They must remain with redistributed packages. Runtime versions are recorded in release validation.

FFmpeg is launched as a separate executable and is not included in this release. Users install their chosen build separately. FFmpeg's licensing depends on configuration and enabled components; see the [official legal information](https://ffmpeg.org/legal.html) and the chosen build's notices. A future bundled decoder must include its matching licenses, notices and required source information; copying only ffmpeg.exe is not a distribution strategy.

The Local Remote Desktop icon is an original repository-owned vector asset, with no downloaded fonts or artwork. Regeneration uses Python/Pillow as a development tool; Pillow is not a runtime dependency.

中文：本项目原创代码、文档与品牌素材采用 GPL-3.0-or-later，除非文件另有声明；完整条款见 LICENSE，软件不提供担保。后续二进制包附许可证、声明、标明精确提交的 SOURCE.txt，以及含项目相应源码和构建脚本的 source.zip；不依赖私有仓库链接提供源码。既有发布附件及仓库可见性不变。Windows 包中的 .NET/桌面运行时及其他第三方组件保留各自许可。FFmpeg 仍单独安装，其许可取决于所选构建；Pillow 仅用于开发生成图标。
