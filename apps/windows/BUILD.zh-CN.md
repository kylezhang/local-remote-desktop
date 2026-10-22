# Windows 构建和使用

[English](BUILD.md)

Windows 10/11 x64，开发需要 .NET 8 SDK，无第三方 NuGet 包。仓库根目录执行：

```powershell
dotnet build apps/windows/LocalRemoteDesktop.csproj -c Release
dotnet run --project apps/windows/LocalRemoteDesktop.csproj -c Release
dotnet publish apps/windows/LocalRemoteDesktop.csproj -c Release -r win-x64 --self-contained true -o dist/windows
```

publish 独立包包含 .NET Desktop Runtime，普通 build 程序仍依赖安装的运行时。保持完整输出目录，运行 LocalRemoteDesktop.exe。已发布 v0.1.0 压缩包仍使用 RemoteMacLAN.exe，旧发版附件不修改。

新版源码打包时，程序直接位于 ZIP 第一层。请使用“全部解压”到新的较短目录，再运行
`LocalRemoteDesktop.exe`；不要在压缩包中运行或只复制 EXE。路径过深可能导致 Windows
解压中断，留下 EXE 却缺少运行库。启动失败时，可双击包内 `CHECK-INSTALLATION.cmd`，
按文件清单检查缺失、损坏或路径过长；不会启动应用或修改系统设置。它仅为该次子进程
允许执行包内的本地 PowerShell 检查脚本，不改变系统执行策略。文件清单用于发现解压不完整；
独立核验原始 ZIP 需比对发行页的 `SHA256SUMS.txt`。两者都不是发布者代码签名。

在干净且已提交的检出目录运行 `./scripts/package-windows.ps1`，可得到含第三方许可、
对应源码和文件清单的完整 ZIP。CI 会把最终 ZIP 解压到隔离目录，逐文件核验，再以
`--check-startup <report.json>` 启动解压后的 EXE。这是显式启用的后台诊断：加载实际
程序入口、运行库和不显示的首页资源后退出，不连接、不共享、不读取保存的凭据或剪贴板，
也不显示窗口。正常启动仍会打开应用。后续客户端回归使用解压包内的程序集，而非打包前目录。

FFmpeg 不打包，按 [官方入口](https://ffmpeg.org/download.html) 安装含 H.264 解码器的完整构建并配置 PATH，或将程序及所需 DLL 放到客户端旁。已有正常安装不必重复配置。尚无硬件解码和 D3D11。

首次地址默认 Windows 本机局域网 IPv4，方便改同网段，并非发现 Mac。优先恢复记忆的 IP/密钥，按连接时用当前用户 DPAPI 加密保存到 `%LOCALAPPDATA%/LocalRemote`；格式有效不等于认证成功。忘记连接清除所选项。

首次启动改名版本时，从 `%LOCALAPPDATA%/RemoteMacLAN` 一次性复制已有加密记录，保留原文件，不覆盖新配置。完成标记避免忘记的连接被重新迁入。

点击画面获得键盘焦点，F11 全屏、Ctrl+Alt+Esc 释放输入。点击诊断按钮后 KeyboardFocus=false 正常，HostPosted 是 CGEvent 提交数量，不是目标接受确认。远程画面禁用 Windows IME 组合输入，物理事件交给 Mac 输入法。

开发构建默认启用“Windows 快捷键”：Ctrl→Command、Win→Control，同时转换按键与修饰标志；取消勾选恢复原生映射。选择用 DPAPI 本地记忆，物理 Ctrl+Alt+Esc 在两种模式下都用于释放。此修复只需更新 Windows，兼容现有 Mac 协议；此快捷键映射本身不增加跨设备同步；测试版另有记忆的文字/文件共享开关，见使用指南。

连接中可切画质，约三秒按新档位自动认证重连，快速切换采用最后选择，关闭自动重连也可切换；单独使用此功能无需更新 Mac 协议。

当前源码输出位于 `apps/windows/bin/Release/net8.0-windows/win-x64`。更新前关闭运行实例，部署不清除连接记忆。
已有 `windows-client/bin/.../RemoteMacLAN.exe` 快捷路径可保留为本地兼容启动位置，不是新源码工程或发布程序名。
