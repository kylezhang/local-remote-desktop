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

启动时地址框自动填入 Windows 当前的局域网 IPv4，方便改成同网段 Mac 的地址；优先选有 IPv4 网关的已连接以太网/Wi-Fi 网卡，排除回环和链路本地地址。无法确定时留空，不保留固定示例 IP。此默认地址不是自动发现的 Mac 地址。

点击连接时自动记住填写的 IP 和格式有效的配对密钥，即使 Mac 暂时离线也可保留；下次启动优先恢复这组参数。两项均用当前 Windows 用户的 DPAPI 加密存储，不保存明文。格式校验不等于配对验证，错密钥仍会被 Host 拒绝。首次无记忆时才使用本机局域网 IPv4。点击“忘记连接”清除当前地址对应的密钥及匹配的上次地址记录。
