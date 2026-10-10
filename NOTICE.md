# Dependencies and distribution

Local Remote Desktop currently has no declared project-wide open-source license. The repository remains private; this release does not change visibility or license rights.

Windows self-contained packages include Microsoft .NET 8 and Windows Desktop runtime components. The release packager copies the runtime licenses and third-party notices from the exact installed .NET runtime packs/SDK used for that build. They must remain with redistributed packages. Runtime versions are recorded in release validation.

FFmpeg is launched as a separate executable and is not included in this release. Users install their chosen build separately. FFmpeg's licensing depends on configuration and enabled components; see the [official legal information](https://ffmpeg.org/legal.html) and the chosen build's notices. A future bundled decoder must include its matching licenses, notices and required source information; copying only ffmpeg.exe is not a distribution strategy.

The Local Remote Desktop icon is an original repository-owned vector asset, with no downloaded fonts or artwork. Regeneration uses Python/Pillow as a development tool; Pillow is not a runtime dependency.

中文：仓库保持私有，尚未选择项目开源许可证。Windows 包包含 .NET/桌面运行时，必须保留相应许可证与第三方说明。FFmpeg 不包含在本次包中，用户单独安装并遵守所选构建许可。图标为本项目原创矢量设计；Pillow 仅用于开发生成，不是运行依赖。
