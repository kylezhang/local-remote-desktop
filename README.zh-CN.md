<p align="center">
  <img src="assets/brand/icon.png" alt="LocalRemote" width="112" />
</p>

# LocalRemote

**局域网内的跨设备远程控制。**

LocalRemote 让你通过局域网查看并操作另一台电脑当前的桌面。设备直接连接，无需账号或云中转；回到被控电脑时，继续同一个工作现场。

**当前支持 Windows → Mac。** 双向桌面控制和手机控制端正在规划中。

[English](README.md) | **简体中文**

[下载](https://atomgit.com/zkxw2008/LocalRemote/releases) · [文档](#文档) · [路线图](docs/zh-CN/roadmap.md) · [更新日志](CHANGELOG.zh-CN.md)

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

当前支持单控制端、单显示器。Mac 需要保持唤醒并登录；不支持 FileVault 解锁及登录前控制，锁屏控制不保证。应用界面目前为中文，英文界面已列入计划。

## 安装

### Windows

1. 从 [Releases](https://atomgit.com/zkxw2008/LocalRemote/releases) 下载 Windows x64 ZIP 并解压。
2. 完整安装支持 H.264 解码的 [FFmpeg 构建](https://ffmpeg.org/download.html)，并加入 PATH。
3. 运行解压目录中的 `RemoteMacLAN.exe`。

发布包包含 .NET Desktop Runtime。已有能正常使用的 FFmpeg 可继续沿用。程序文件名暂时保留兼容名称。

### macOS

准备 Xcode Command Line Tools（需要时执行 `xcode-select --install`），然后构建安装：

```sh
git clone https://atomgit.com/zkxw2008/LocalRemote.git
cd LocalRemote
bash macos-host/install-app.sh
```

在系统设置 → 隐私与安全性授权**屏幕录制**和**辅助功能**，授权后退出重开应用，再开始共享。以正常用户执行安装，不使用 `sudo`。

默认安装会清理本应用这两项授权。日常更新若要保留已正常工作的权限，可加 `--keep-permissions`。

Mac 当前提供源码安装。签名、升级、权限与排障见 [Mac 指南](docs/zh-CN/macos.md)。

## 使用

1. 在 Mac 打开 LocalRemote，开始共享并复制配对密钥。
2. 在 Windows 填写 Mac 的局域网 IP 与配对密钥，点击连接。
3. 点击远程画面后操作桌面，用完断开；Mac 会话保持打开。

两台设备需在同一网络且能够互通，允许 TCP **47980** 到达 Mac。密钥只交给可信设备，在可信局域网使用。

| 快捷键 | 操作 |
|---|---|
| F11 | 切换全屏 |
| Ctrl+Alt+Esc | 释放远程输入 |
| Win 键 | 对应 Mac Command 修饰键 |

切换画质会短暂重连。控件、画质及连接帮助见 [使用指南](docs/zh-CN/usage.md)。

## 路线图

目标是各平台安装同一个产品的对应版本，通过统一界面连接设备或允许连接。接下来先完善安装稳定性、英文界面、配对和设备发现，再推进 Windows ↔ Mac 及手机控制端。

以上是计划功能，具体范围与优先级见 [路线图](docs/zh-CN/roadmap.md)。

## 开发背景

这个项目起源于一个实际需求：找到适合办公室日常使用的 Windows 连接 Mac 客户端。尝试过的工具有些体验不顺手、有些版本陈旧、有些需要付费。借助 AI 辅助开发，我决定从自己每天使用的设备开始，做一个专注于这个问题的小工具。

## 文档

- [使用指南](docs/zh-CN/usage.md)与 [Mac 排障](docs/zh-CN/macos.md)
- [Windows 源码构建](windows-client/BUILD.zh-CN.md)
- [架构](docs/zh-CN/architecture.md)与 [安全协议](docs/zh-CN/protocol.md)
- [构建与测试状态](docs/zh-CN/validation.md)
- [参与维护](CONTRIBUTING.md)与 [发版流程](docs/zh-CN/releasing.md)

## 许可证

项目尚未选择开源许可证。当前分发与依赖说明见 [NOTICE](NOTICE.md)。
