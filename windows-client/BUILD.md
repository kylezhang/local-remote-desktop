# Windows 构建和分发

开发：.NET 8 SDK，Windows 10/11 x64，FFmpeg H.264 decoder。无 NuGet 依赖。

```powershell
dotnet build windows-client/RemoteMacLAN.csproj -c Release
dotnet run --project windows-client/RemoteMacLAN.csproj -c Release
```

发布独立包（在联网开发机器下载微软 runtime pack 后）：

```powershell
dotnet publish windows-client/RemoteMacLAN.csproj -c Release -r win-x64 --self-contained true -o build/windows
```

本次附带的 Windows ZIP 是实际 Release 编译产物，**依赖 .NET 8 Desktop Runtime**，不是 self-contained 包。安装 .NET 8 SDK 也提供所需桌面运行时。FFmpeg 在 PATH 或 exe 同目录；不把 ffmpeg.exe、密钥或用户配置提交到仓库。不安装 .NET Desktop Runtime 时 exe 可能在启动前退出并显示运行时缺失提示。

界面填写 Mac IPv4、64 字符配对密钥，选择质量后连接。Mac 端必须已运行且授权。Ctrl+Alt+Esc 释放输入，F11 全屏。硬件解码/D3D11 尚未实现。
