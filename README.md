# Remote Mac LAN

Windows 台式机通过局域网查看并操作 Apple Silicon MacBook 的**当前真实桌面**。无账号、无公网、无中间服务器。断开后 Mac 保持同一个工作现场。

## 当前状态（2026-10-08）

新增独立的 [WebRTC 只读画面对比实验](experiments/webrtc/README.md)，用于实测是否值得迁移视频链路。原客户端保留；实验不能解决输入权限问题。

现主线继续原生 TCP 客户端，WebRTC 迁移暂停。新增输入诊断、Mac 本机键盘自检和解码延迟优化；Windows 客户端联调与量化比较见 [修复候选验证记录](docs/validation-2026-10-08.md)。Mac 新增代码尚未原生编译或运行，不能称输入故障已彻底解决。

这是待 Mac 实机联调的 0.1 原型，不能把生成代码等同于已经验证可用的远控产品。

- Phase 0：完整 Swift macOS Host、.NET 8 WPF Windows Client、协议及文档。
- Phase 1/2：已实现 ScreenCaptureKit → VideoToolbox 硬件 H.264 → 加密 TCP → FFmpeg → WPF 图像，以及鼠标、滚轮、键盘输入注入。**Windows 编译通过；Mac 编译和两机真实桌面链路尚未验证。**
- Phase 3 部分：全屏、认证成功后的自动重连、登录后 LaunchAgent、三档画质。未实现剪贴板及 mDNS。
- Windows 使用 WPF 是为减少部署与工程复杂度；首版解码使用独立 FFmpeg 子进程，BGRA 显示到 WriteableBitmap。**没有实现 Media Foundation、D3D11 或 Windows 硬件解码**。这是明确的原型性能妥协，`IVideoDecoder` 可替换。
- 标准/高清请求最多 1920×1080、60fps；流畅最多 1280×720、30fps，保持比例。实际采集受桌面变化、设备与网络影响；没有证据表明两机达到 1080p60。

## 最短首次测试

1. Mac 安装 Xcode Command Line Tools（`xcode-select --install`），macOS 13+，Apple Silicon。在 `macos-host` 执行 `bash build-app.sh`，把生成的 `build/RemoteMacLAN.app` 复制到 `/Applications/RemoteMacLAN.app`，用 Finder 打开。
2. 系统设置 → 隐私与安全性：授予 Host **屏幕录制 / 屏幕与系统音频录制**（名称因系统版本变化）和**辅助功能**权限。退出并重新打开 Host，点击开始共享。没有辅助功能权限时只能查看；本程序不采集音频、不写录像文件。
3. Mac 系统设置 → 网络查看局域网 IPv4 地址；允许 Host 的 macOS 防火墙入站连接。两机需处于互通局域网，TCP 47980；访客 Wi-Fi 的客户端隔离会阻断连接。
4. Windows 安装 .NET 8 SDK，准备包含 H.264 解码器的 `ffmpeg.exe`（[FFmpeg 官方下载入口](https://ffmpeg.org/download.html)链接的 Windows 构建）；把 FFmpeg 放 PATH 或客户端 exe 同目录。仓库不分发第三方二进制，遵守所选构建许可证。
5. 在仓库根目录运行 `dotnet build windows-client/RemoteMacLAN.csproj -c Release`；运行 `windows-client/bin/Release/net8.0-windows/RemoteMacLAN.exe`。也可 `dotnet run --project windows-client/RemoteMacLAN.csproj -c Release`。
6. Mac 勾选显示配对密钥，把 **64 位十六进制密钥**安全复制到 Windows 配对框，填写 IP，选“流畅”，连接。先验证画面，再点击画面用鼠标及英文键盘操作 TextEdit。再试标准档、断线重连及 Mac 本地继续工作。

无需另行生成证书或配置服务器。密钥等同控制权限，不放聊天群、截图或仓库。首次需人工从可信 Mac 界面取密钥；点击连接会加密记住 IP 和格式有效的密钥，下次自动填入，点击“忘记连接”可清除。错误密钥不会通过认证或自动重试。

## 操作与权限

- F11 切换全屏；Ctrl+Alt+Esc 释放远端输入并把焦点移回本地按钮。只有画面获焦时发送键盘输入；按钮和密钥框里的按键不发送。
- Windows Ctrl → Mac Control，Alt → Option，Win → Command，Shift → Shift。Mac 的复制是 Win+C。系统保留的 Win 快捷键/Alt+Tab/Ctrl+Alt+Del 不保证送达；本原型没有全局键盘钩子。支持常用字母、数字、标点、方向键、功能键、数字键盘；IME/中文组合输入未实现。
- 鼠标按视频实际显示区域映射，忽略黑边；支持左右键、中键、拖动、垂直滚轮，按系统双击间隔设置 clickState。失焦、断开时释放按住的键和按钮。真实点击/双击行为仍需 Mac 实测。
- 停止共享或退出 Mac Host 可收回控制。窗口关闭不会退出 Host；重新打开应用可用菜单显示窗口。
- 一个控制端、主显示器、IPv4 私有/链路本地/回环地址，固定端口；不支持 IPv6、音频、多显示器、文件传输、摄像头、录屏、移动端、公网穿透、TURN/STUN。

## 登录后自动启动

先完成 `/Applications` 下应用的授权和手动测试，再执行 `bash macos-host/install-launch-agent.sh`。用户 LaunchAgent 在 Aqua 用户会话登录后启动 Host；退出用户/休眠/网络中断会断线。Windows 已成功配对后每 3 秒尝试重连，可取消。

**不能绕过 FileVault Preboot，不能保证用户未登录时控制系统登录界面。**重启后需正常解锁并进入用户桌面才可连接。合盖导致 Mac 休眠时不能连接；不修改系统电源策略。

卸载自启动：`launchctl bootout gui/$(id -u) ~/Library/LaunchAgents/com.remotemaclan.host.plist`，再删除该 plist。脚本仅设置 RunAtLoad，不会在用户主动退出后强制重启。

## 安全

Mac Keychain 生成并保存 256 位随机共享密钥；Windows 使用当前用户 DPAPI 按 IP 保存。每连接双方随机 nonce，HMAC-SHA256 双向认证并派生方向独立密钥；AES-256-GCM 加密画面和控制，长度头作为 AAD，隐式递增计数器防重放。认证前不启动捕获或接受输入，无未认证开发模式。首次取密钥需要可信的带外渠道。

密钥轮换：停止并退出 Host，执行 `security delete-generic-password -s com.remotemaclan.host -a pairing-v1`，重新打开后生成新密钥，Windows 重新填写。旧密钥失效。Windows 保存位于 `%LOCALAPPDATA%/RemoteMacLAN/*.dpapi`；删除可清除配对。安全协议未经独立审计，没有前向保密；获知长期密钥并录到握手的人可解密历史流量。未认证连接最多等待 15 秒，会暂时占用单连接槽，仍存在局域网拒绝服务风险。

## 验证

本次 Windows x64 使用 .NET SDK 8.0.425 实际 Release 构建，零警告零错误。测试程序已通过双向认证、碎片化 TCP、篡改/重放/错误密钥/超长报文拒绝、实际 H.264→BGRA 解码、DPAPI 存储。详见 [构建记录](docs/build-report.md)。运行：

```powershell
dotnet run --project tests/ProtocolChecks/ProtocolChecks.csproj -c Release
```

测试要求 FFmpeg 在 PATH。它是合成协议端，不替代 Mac 联调。Mac Swift/Apple SDK 无法在本 Windows 环境原生构建，进行了源码静态检查，尚未确认 SDK 类型检查、TCC 授权、真实捕获、硬件编码、CGEvent、LaunchAgent。源码包含真实 API 调用，仍可能需要在目标 Mac 修正 SDK 差异。请按 [验收清单](docs/validation.md) 联调。

重新构建时临时 ad-hoc 签名可能使 macOS 权限需要重授；稳定使用建议设 `SIGN_IDENTITY` 为本人的 Apple 开发签名身份后构建。不承诺免授权/免重授。

架构见 [architecture](docs/architecture.md)，线路协议见 [protocol](docs/protocol.md)，后续工作见 [roadmap](docs/roadmap.md)。

如 Command Line Tools 构建遇到 `SwiftUIMacros.StateMacro` 缺失，请更新源码；修正版已移除这处宏依赖并使用原生构建后端，见 [macOS 构建说明](docs/macos-build-troubleshooting.md)。

后续更新 Mac 推荐 `git pull` → `bash macos-host/build-app.sh` → `bash macos-host/install-app.sh`。安装脚本退出旧应用后替换并重开；构建优先复用已有开发签名证书。无开发证书的 ad-hoc 构建不能保证更新后保留隐私权限。配对密钥现已独立于屏幕权限加载，并支持复制。
