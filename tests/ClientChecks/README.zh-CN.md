# Windows 客户端后台检查

[English](README.md)

需要 Windows、.NET 8 SDK/Desktop 运行时，以及 PATH 中可用、支持 H.264 的完整
FFmpeg（`ffmpeg.exe`）。在仓库根目录执行：

```powershell
ffmpeg -version
dotnet run --project tests/ClientChecks -c Release
```

默认检查不显示或激活窗口，使用未显示的 WPF 布局、隔离设置/存储、记录输入/剪贴板、
加密回环对端和合成视频，不捕获用户桌面或注入输入。画面检查通过 FFmpeg 生成、解码
小型测试图案；文件/TCP 检查使用可删除的临时文件和本机回环端口。

`LOCALREMOTE_INTERACTIVE_CHECKS`、`LOCALREMOTE_NATIVE_INPUT_CHECK`、
`LOCALREMOTE_LAN_INPUT_CHECK` 是独立的主动启用路径，日常后台运行不要设置；
真实交互须约定测试时间并使用专用目标。无窗口焦点/窗口检查不代表真实系统焦点、
全屏显示器几何或双机验收通过。

验证最终包可通过 MSBuild 的 `ClientAssemblyPath` 属性指定其程序集，包内保留
对应源码及许可。检查结果和待完成原生门槛见[验证记录](../../docs/zh-CN/validation.md)。
