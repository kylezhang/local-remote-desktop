<p align="center">
  <img src="assets/brand/icon.png" alt="Local Remote Desktop" width="112" />
</p>

# Local Remote Desktop

[English](README.md) | **简体中文**

## 背景

这个项目起源于办公室里的一个日常需求：用 Windows 台式机远程操作 Mac。尝试过一些工具后，始终没有找到足够顺手的客户端：有的使用体验不够理想，有的版本较旧，还有的需要付费。对于个人在局域网内连接自己的两台设备这样一个简单需求，我更希望有个轻便、好用的工具，不必为此额外付费。

于是，借助 AI 辅助开发，我决定自己动手做一个局域网远程桌面工具，从这个日常需求出发，让连接和操作都尽量简单。

## 介绍

Local Remote Desktop 让你通过局域网查看并操作另一台电脑当前的桌面。设备直接连接，无需账号或云中转。回到被控电脑时，可以继续同一个工作现场。

当前 alpha 版提供 **Windows ↔ Mac** 桌面角色：每个平台使用一个应用，既可连接另一台设备，也可接受连接。手机控制端、Linux 和 HarmonyOS 仍在规划中，尚未提供。

## 功能

- **操作当前桌面**：从另一台电脑连接 Windows 或 Mac，使用键盘和鼠标控制目标桌面。
- **局域网配对连接**：连接经过认证与加密，配对信息安全保存在各自设备上。
- **记住连接**：保留目标地址和密码，方便下次连接。
- **桌面控制**：全屏、自动重连，以及连接中也可调整的三档画质。
- **方便配对**：支持自动生成或手填密码；原有密钥保留，直到主动修改。
- **文字与文件剪贴板**：在已配对设备间复制粘贴纯文本和普通文件，传输时无需重复配对。

alpha 版还包含附近设备选择和中英文界面。两端角色均已实现，但完整的双机权限、剪贴板和升级验收仍待完成。具体行为和范围见 [使用指南](docs/zh-CN/usage.md)。

## 安装

最新下载为 **v0.3.0-alpha.3（预发布）**，可从 [AtomGit](https://atomgit.com/zkxw2008/local-remote-desktop/releases/tag/v0.3.0-alpha.3) 或 [GitHub](https://github.com/kylezhang/local-remote-desktop/releases/tag/v0.3.0-alpha.3) 获取。两站提供相同的 Windows 和 Apple Silicon Mac 包、对应源码及 SHA-256 校验文件。历史版本保留，v0.1.0 是此前的正式版基线。

### Windows

要求 **Windows 10/11，x64**。

1. 下载并解压 Windows ZIP。
2. 安装支持 H.264 解码的 [FFmpeg](https://ffmpeg.org/download.html)，让 `ffmpeg.exe` 可通过 PATH 找到，或放在应用旁边。已有能正常使用的安装可继续沿用。
3. 运行 `LocalRemoteDesktop.exe`。

保留解压后的完整目录。发布包包含 .NET Desktop Runtime，无需另行安装。

### macOS

要求 **macOS 13+、Apple Silicon**；使用下载包无需本地编译器或开发者会员。

1. 下载并打开 Mac DMG；替换前退出正在运行的旧程序。
2. 将 **Local Remote Desktop.app** 拖到 **Applications**，再打开安装后的应用。
3. macOS 14 及以上接受连接时，在系统选择器中选择主屏幕；macOS 13 需屏幕录制授权。键鼠控制还需单独的辅助功能授权。

Mac 包为**临时签名、未公证**，可能需系统首次打开确认，更新后也可能重新认可辅助功能/钥匙串。不要关闭 Gatekeeper，也不要为了重开应用而反复安装。新的屏幕选择与升级行为尚未完成实机验收，安装和排障见 [Mac 指南](docs/zh-CN/macos.md)。

## 使用

1. 在要被控制的电脑上选择**接受连接**，开启共享并取得配对密码。
2. 在另一台电脑上选择**发起连接**，选择附近设备或填写**目标 IP**，使用该密码连接。
3. 点击远程画面后使用键盘和鼠标。用完断开，目标电脑的工作会话继续保留。

两台设备需在私有网络中直连可达，允许 TCP **47980** 到达目标电脑（文件传输另需 **47981**）。配对密码只交给可信设备。

Windows 上 **F11** 切换全屏，**Ctrl+Alt+Esc** 释放远程输入。启用 Windows 快捷键时，Ctrl+C/V 对应 Mac 的 Command+C/V。支持剪贴板共享的已配对连接默认开启同步，可在“设置 → 剪贴板”中调整。支持的文件复制粘贴格式见 [文件指南](docs/zh-CN/files.md)。

当前支持单控制端和主显示器。目标电脑需保持唤醒并登录；不支持登录界面控制、安全桌面绕过或 FileVault 预启动解锁，锁屏控制不保证。

## 开发

macOS 使用 **Swift**，Windows 使用 **C# / .NET 8 / WPF**。Mac 通过 ScreenCaptureKit 采集画面、VideoToolbox 编码，Windows 控制端目前通过 FFmpeg 解码。设备通过局域网内经过认证和加密的 TCP 连接通信，详见 [架构](docs/zh-CN/architecture.md)。

GitHub Actions 是主要编译和自动测试环境，源码从 AtomGit 同步后自动构建 Windows x64
和 Apple Silicon Mac 预览包。参见[在线构建与下载](docs/zh-CN/continuous-integration.md)。
本地编译可选；准备 .NET 8 SDK，在仓库根目录构建 Windows：

```powershell
dotnet build apps/windows/LocalRemoteDesktop.csproj -c Release
dotnet run --project apps/windows/LocalRemoteDesktop.csproj -c Release
```

在 macOS 上构建应用：

```sh
bash apps/macos/build-app.sh
```

两端原生编译、后台检查、不显示窗口的中英文布局及自动发版同步已通过 GitHub Actions。[发版记录](tests/results/automated-release-2026-10-14.md)分别记载 CI 证据、此前用户反馈的桌面控制及尚未完成的实机验收。主分支更新不覆盖已有发行包；准备发版前请查看 [验证状态](docs/zh-CN/validation.md)与 [参与维护](CONTRIBUTING.md)。

## 文档

- [产品方向](docs/zh-CN/product.md) · [v0.3.0 交付计划](docs/zh-CN/v0.3.0-plan.md) · [界面设计方案](docs/zh-CN/design.md)

- [使用指南](docs/zh-CN/usage.md) · [文件剪贴板](docs/zh-CN/files.md)
- [Mac 控制端预览](docs/zh-CN/mac-controller.md) · [Windows 共享预览](docs/zh-CN/windows-sharing.md)
- [Mac 安装与排障](docs/zh-CN/macos.md) · [Windows 源码构建](apps/windows/BUILD.zh-CN.md)
- [路线图与版本规划](docs/zh-CN/roadmap.md) · [更新日志](CHANGELOG.zh-CN.md)
- [架构](docs/zh-CN/architecture.md) · [协议](docs/zh-CN/protocol.md) · [性能](docs/zh-CN/performance.md)
- [多端界面规范](docs/zh-CN/interface.md) · [工程结构](docs/zh-CN/naming.md)
- [开发约定与经验](docs/zh-CN/development-handbook.md)
- [在线构建与下载](docs/zh-CN/continuous-integration.md)
- [验证状态](docs/zh-CN/validation.md) · [发版流程](docs/zh-CN/releasing.md)

## 许可

Local Remote Desktop 采用 [GNU 通用公共许可证第 3 版或更高版本](LICENSE)（`GPL-3.0-or-later`）。允许使用、修改和再分发，包括商业使用；分发受 GPL 约束的修改版本或二进制时，需遵守相应源码提供及同许可证要求。

第三方组件保留各自许可证。版权、依赖与分发说明见 [NOTICE](NOTICE.md)。
