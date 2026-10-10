<p align="center">
  <img src="assets/brand/icon.png" alt="Local Remote Desktop" width="112" />
</p>

# Local Remote Desktop

主分支为 **v0.3.0-alpha.1 开发版**，加入 [Windows 共享预览](docs/zh-CN/windows-sharing.md)、
[局域网设备选择](docs/zh-CN/discovery.md)与中英文切换，遵循[多端界面规范](docs/zh-CN/interface.md)。
Mac 控制端是下一阶段，尚未实现；Mac 原生验收待完成，不替换已发布测试包。

**局域网内的跨设备远程控制。**

Local Remote Desktop 让你通过局域网查看并操作另一台电脑当前的桌面。设备直接连接，无需账号或云中转；回到被控电脑时，继续同一个工作现场。

**当前支持 Windows → Mac。** 双向桌面控制和手机控制端正在规划中。

**测试版：v0.2.0-beta.1**，集中交付短密码、文字/文件剪贴板和更清晰的 Mac 界面。提供 Windows 包与 Mac 源码，Mac 原生验收待完成；最新正式版仍为 v0.1.0。

[English](README.md) | **简体中文**

[下载](https://atomgit.com/zkxw2008/local-remote-desktop/releases) · [文档](#文档) · [路线图](docs/zh-CN/roadmap.md) · [更新日志](CHANGELOG.zh-CN.md)

## 功能

- **操作当前桌面**：查看画面，使用键盘和鼠标。
- **局域网直连**：同一网络中的设备直接连接。
- **可信设备配对**：连接经过认证与加密，配对密钥安全存储。
- **方便日常使用**：记住 Mac 地址和密钥，支持全屏及断线重连。
- **调整画面质量**：流畅、标准、高清三档，连接后也可切换。

## 平台支持

| 平台 | 用途 | 系统要求 |
|---|---|---|
| Windows | 控制 Mac | Windows 10/11，x64 |
| macOS | 共享当前桌面 | macOS 13+，Apple Silicon |

当前支持单控制端、单显示器。Mac 需要保持唤醒并登录；不支持 FileVault 解锁及登录前控制，锁屏控制不保证。已发布包为中文界面；主分支开发版可切换英文。

## 安装

### Windows

1. 从 [Releases](https://atomgit.com/zkxw2008/local-remote-desktop/releases) 下载 Windows x64 ZIP 并解压。
2. 完整安装支持 H.264 解码的 [FFmpeg 构建](https://ffmpeg.org/download.html)，并加入 PATH。
3. 运行所下载版本的程序：v0.2.0-beta.1 为 `LocalRemote.exe`，v0.1.0 为 `RemoteMacLAN.exe`；当前源码构建为 `LocalRemoteDesktop.exe`。

发布包包含 .NET Desktop Runtime。已有能正常使用的 FFmpeg 可继续沿用。旧发布包保留当时的程序文件名；当前源码和后续发布包使用 LocalRemoteDesktop.exe。

### macOS

准备 Xcode Command Line Tools（需要时执行 `xcode-select --install`），然后构建安装：

```sh
git clone https://atomgit.com/zkxw2008/local-remote-desktop.git
cd local-remote-desktop
bash apps/macos/install-app.sh
```

在系统设置 → 隐私与安全性授权**屏幕录制**和**辅助功能**，授权后退出重开应用，再开始共享。以正常用户执行安装，不使用 `sudo`。

默认安装会清理本应用这两项授权。新旧签名兼容时可加 `--keep-permissions`；不兼容时会在改变应用及权限前停止。临时签名开发构建更新前请查看 Mac 指南。

Mac 当前提供源码安装。签名、升级、权限与排障见 [Mac 指南](docs/zh-CN/macos.md)。

## 使用

1. 在 Mac 打开 Local Remote Desktop，开始共享并复制配对密钥。
2. 在 Windows 填写 Mac 的局域网 IP 与配对密钥，点击连接。
3. 点击远程画面后操作桌面，用完断开；Mac 会话保持打开。

两台设备需在同一网络且能够互通，允许 TCP **47980** 到达 Mac。密钥只交给可信设备，在可信局域网使用。

| 快捷键 | 操作 |
|---|---|
| F11 | 切换全屏 |
| Ctrl+Alt+Esc | 释放远程输入 |
| Ctrl 键（Windows 快捷键启用） | 对应 Mac Command 修饰键 |

开发分支默认启用“Windows 快捷键”，Ctrl+C/V 操作 Mac 自己的剪贴板；取消后恢复 Ctrl→Control、Win→Command。已发布的 v0.1.0 安装包仍使用原映射。

开发构建已加入**双向文字与文件剪贴板共享**：已配对连接中默认开启，两端记住主动关闭的选择；可在“设置 → 剪贴板”调整。等显示已启用后重新复制。文字最多 32 KiB；普通文件自动传输，有进度和取消，不重复配对、不逐文件弹窗。Windows 加密模拟端及界面验证通过，Mac 原生验收待完成；已发布 v0.1.0 安装包没有剪贴板共享。见[使用指南](docs/zh-CN/usage.md#文本剪贴板同步开发更新)与[文件指南](docs/zh-CN/files.md)。

开发构建支持手填 **8–64 位配对密码**，或自动生成 **12 位密码**。在 Mac 的“当前密码”旁点击“修改密码”，在独立编辑窗口填写或生成后点击“保存密码”。原有 64 位密钥不会自动变化；使用短密码前需更新两端。详见[使用指南](docs/zh-CN/usage.md#配对密码开发更新)。

切换画质会短暂重连。控件、画质及连接帮助见 [使用指南](docs/zh-CN/usage.md)。

## 路线图

目标是各平台安装同一个产品的对应版本，通过统一界面连接设备或允许连接。接下来先完善安装稳定性、英文界面、配对和设备发现，再推进 Windows ↔ Mac 及手机控制端。

原定 v0.1.1/v0.1.2 的安装与输入改进集中进入 **v0.2.0-beta.1** 测试版；**v0.2.0** 正式版目标为通过验证的发现/剪贴板/本地化、**v0.3.0** 桌面双向控制，随后 **v1.0.0** 稳定桌面版、**v1.1–v1.2** Android/iOS 控制端、**v1.3.0** Linux 桌面版、**v1.4.0** HarmonyOS 手机/平板控制端。这些功能尚未发布，具体范围及发布标准见 [路线图](docs/zh-CN/roadmap.md)。

## 开发背景

这个项目起源于一个实际需求：找到适合办公室日常使用的 Windows 连接 Mac 客户端。尝试过的工具有些体验不顺手、有些版本陈旧、有些需要付费。借助 AI 辅助开发，我决定从自己每天使用的设备开始，做一个专注于这个问题的小工具。

## 文档

- [使用指南](docs/zh-CN/usage.md)与 [Mac 排障](docs/zh-CN/macos.md)
- [Windows 源码构建](apps/windows/BUILD.zh-CN.md)
- [架构](docs/zh-CN/architecture.md)与 [安全协议](docs/zh-CN/protocol.md)
- [构建与测试状态](docs/zh-CN/validation.md)
- [资源占用与性能](docs/zh-CN/performance.md)
- [参与维护](CONTRIBUTING.md)与 [发版流程](docs/zh-CN/releasing.md)
- [工程结构与命名迁移](docs/zh-CN/naming.md)

## 许可证

项目尚未选择开源许可证。当前分发与依赖说明见 [NOTICE](NOTICE.md)。
