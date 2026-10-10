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

当前连接方向为 **Windows → Mac**。长期目标是各平台安装同一个产品的对应版本，既能连接其他设备，也能允许桌面被控制。后续计划支持 Windows ↔ Mac、手机控制端、Linux 和 HarmonyOS。

## 功能

- **操作当前桌面**：查看 Mac 画面，使用 Windows 的键盘和鼠标。
- **局域网配对连接**：连接经过认证与加密，配对信息安全保存在各自设备上。
- **记住连接**：保留目标地址和密码，方便下次连接。
- **桌面控制**：全屏、自动重连，以及连接中也可调整的三档画质。
- **方便配对**：支持自动生成或手填密码；原有密钥保留，直到主动修改。
- **文字与文件剪贴板**：在已配对设备间复制粘贴纯文本和普通文件，传输时无需重复配对。

短密码和剪贴板共享已包含在测试版中；v0.1.0 使用原有密钥，不含剪贴板共享。局域网设备选择和中英文切换已加入当前开发版。具体行为和范围见 [使用指南](docs/zh-CN/usage.md)。

## 安装

最新正式版为 **v0.1.0**，已发布测试版为 **v0.2.0-beta.1**。Windows 安装包可从 [Releases](https://atomgit.com/zkxw2008/local-remote-desktop/releases) 下载；Mac 当前需要从源码构建。

### Windows

要求 **Windows 10/11，x64**。

1. 下载并解压 Windows ZIP。
2. 安装支持 H.264 解码的 [FFmpeg](https://ffmpeg.org/download.html)，让 `ffmpeg.exe` 可通过 PATH 找到，或放在应用旁边。已有能正常使用的安装可继续沿用。
3. 测试版运行 `LocalRemote.exe`，v0.1.0 运行 `RemoteMacLAN.exe`；当前源码构建使用 `LocalRemoteDesktop.exe`。

保留解压后的完整目录。发布包包含 .NET Desktop Runtime，无需另行安装。

### macOS

要求 **macOS 13+、Apple Silicon** 和 **Xcode Command Line Tools**。需要时先执行 `xcode-select --install`，再运行：

```sh
git clone https://atomgit.com/zkxw2008/local-remote-desktop.git
cd local-remote-desktop
bash apps/macos/install-app.sh
```

安装脚本会构建当前主分支，并打开 `/Applications/Local Remote Desktop.app`。以正常用户执行，不使用 `sudo`。

在系统设置 → 隐私与安全性中允许**屏幕录制**和**辅助功能**，授权后退出并重新打开应用。默认安装会重置本应用的授权记录；升级、签名和保留权限的安装方式见 [Mac 指南](docs/zh-CN/macos.md)。

## 使用

1. 在 Mac 打开应用，开始共享，并复制配对密码。
2. 在 Windows 的**目标 IP**中填写 Mac 的局域网地址，输入相同密码后连接。开发版也可以选择附近的 Mac。
3. 点击远程画面后使用键盘和鼠标。用完断开，Mac 会话继续保留。

两台设备需在同一网络中互通，并允许 TCP **47980** 到达 Mac。配对密码只交给可信设备。

**F11** 切换全屏，**Ctrl+Alt+Esc** 释放远程输入。启用 Windows 快捷键时，Ctrl+C/V 对应 Mac 的 Command+C/V。支持剪贴板共享的已配对连接默认开启同步，可在“设置 → 剪贴板”中调整。支持的文件复制粘贴格式见 [文件指南](docs/zh-CN/files.md)。

当前支持单控制端、单显示器。Mac 需要保持唤醒并登录；不支持登录界面控制或 FileVault 预启动解锁，锁屏控制不保证。

## 开发

macOS 使用 **Swift**，Windows 使用 **C# / .NET 8 / WPF**。Mac 通过 ScreenCaptureKit 采集画面、VideoToolbox 编码，Windows 控制端目前通过 FFmpeg 解码。设备通过局域网内经过认证和加密的 TCP 连接通信，详见 [架构](docs/zh-CN/architecture.md)。

准备 .NET 8 SDK，在仓库根目录构建 Windows：

```powershell
dotnet build apps/windows/LocalRemoteDesktop.csproj -c Release
dotnet run --project apps/windows/LocalRemoteDesktop.csproj -c Release
```

在 macOS 上构建应用：

```sh
bash apps/macos/build-app.sh
```

主分支为 **v0.3.0-alpha.2**，包含 [Windows 共享预览](docs/zh-CN/windows-sharing.md)。[Mac 控制端预览](docs/zh-CN/mac-controller.md)已在源码中实现，并完成传输与状态自动检查；Mac 原生控制端构建、画面及真实 Mac → Windows 验收仍待完成。Mac 原生构建与运行验收仍待完成；开发分支不替换已有发布包。准备发版前请查看 [验证状态](docs/zh-CN/validation.md)与 [参与维护](CONTRIBUTING.md)。

## 文档

- [使用指南](docs/zh-CN/usage.md) · [文件剪贴板](docs/zh-CN/files.md)
- [Mac 控制端预览](docs/zh-CN/mac-controller.md) · [Windows 共享预览](docs/zh-CN/windows-sharing.md)
- [Mac 安装与排障](docs/zh-CN/macos.md) · [Windows 源码构建](apps/windows/BUILD.zh-CN.md)
- [路线图与版本规划](docs/zh-CN/roadmap.md) · [更新日志](CHANGELOG.zh-CN.md)
- [架构](docs/zh-CN/architecture.md) · [协议](docs/zh-CN/protocol.md) · [性能](docs/zh-CN/performance.md)
- [多端界面规范](docs/zh-CN/interface.md) · [工程结构](docs/zh-CN/naming.md)
- [验证状态](docs/zh-CN/validation.md) · [发版流程](docs/zh-CN/releasing.md)

## 许可

Local Remote Desktop 采用 [GNU 通用公共许可证第 3 版或更高版本](LICENSE)（`GPL-3.0-or-later`）。允许使用、修改和再分发，包括商业使用；分发受 GPL 约束的修改版本或二进制时，需遵守相应源码提供及同许可证要求。

第三方组件保留各自许可证。版权、依赖与分发说明见 [NOTICE](NOTICE.md)。
