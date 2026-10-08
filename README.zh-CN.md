<img src="assets/brand/icon.png" alt="LocalRemote 图标" width="96" />

# LocalRemote

**局域网内的跨设备远程控制。连接自己的设备，继续同一个工作现场。**

[English](README.md) · [路线图](docs/zh-CN/roadmap.md) · [更新日志](CHANGELOG.zh-CN.md) · [发布下载](https://atomgit.com/zkxw2008/LocalRemote/releases)

## 为什么开发它

我想在办公室里，用 Windows 台式机操作同一局域网中的 Mac，但尝试过的工具没有完全满足需求：有些体验不顺手，有些客户端版本陈旧，有些需要付费。我希望有一个简单的小工具，专门解决这个日常问题，不引入账号和云服务器。

在 AI 辅助开发的条件下，我决定自己动手做 LocalRemote。第一步就是从 Windows 查看、操作 Mac 当前桌面，断开后拿起 Mac，继续原来的工作。

## v0.1.0 当前能力

当前仅支持 **Windows 10/11 x64 → Apple Silicon macOS 13+**。这是首次以 LocalRemote 名称发布；此前 0.2.x 属于开发迭代，本次两端统一为 0.1.0，已有功能不会因此减少。

- 共享已登录的真实桌面；支持鼠标、拖动、滚轮和物理键盘输入。
- 手填 Mac IPv4，局域网直连，无账号、无公网中转、无中心服务器。
- 共享密钥配对、双向认证、画面与操作加密；Windows 加密记住 IP 和密钥。
- 全屏、失焦释放输入、认证后的自动重连；连接中切画质会短暂断开后自动重连。
- Mac 登录后自启动（可选 LaunchAgent）、权限诊断和本机键盘自检。

流畅请求最多 720p/30fps/4Mbps；标准最多 1080p/60fps/10Mbps；高清同尺寸与帧率，18Mbps。它们是请求上限，不是实测性能保证。先使用流畅；Windows 当前仍是 FFmpeg 软件解码和 WPF 显示。

## 最短上手步骤

1. Mac 准备 Xcode Command Line Tools（需要时执行 `xcode-select --install`）。克隆后在仓库根目录用正常登录用户运行安装脚本，不使用 sudo：

```sh
git clone https://atomgit.com/zkxw2008/LocalRemote.git
cd LocalRemote
bash macos-host/install-app.sh
```

2. 系统设置 → 隐私与安全性，授权“屏幕录制 / 屏幕与系统音频录制”和“辅助功能”。授权后退出重开应用，不要为了重开再次安装。点击“开始共享”，显示或复制配对密钥。程序不采集音频、不写录像文件。安装脚本先构建，完整替换旧应用包，保留密钥，默认不重置屏幕权限。
3. Windows 下载发布页中的 x64 ZIP，解压后运行 `RemoteMacLAN.exe`。包内包含 .NET Desktop Runtime，**FFmpeg 需要另行准备**：使用 [官方入口](https://ffmpeg.org/download.html) 提供的含 H.264 解码器的构建，完整安装到 PATH，或把可执行文件及所需 DLL 放到客户端旁。已有能用的 FFmpeg 不必重复安装。源码构建见 [Windows 说明](windows-client/BUILD.zh-CN.md)。
4. 查看 Mac 网络设置中的局域网 IPv4，允许 TCP 47980 入站。两台设备需互通，访客 Wi-Fi 隔离可能阻断连接。在 Windows 填入 Mac IP、64 位十六进制配对密钥，选流畅后连接。先点击远程画面，再在普通文本编辑器中测试输入。

F11 全屏；Ctrl+Alt+Esc 释放远程输入。Ctrl/Alt/Win 对应 Mac Control/Option/Command，例如 Win+C 是 Mac 的复制。Windows 保留的系统快捷键不保证转发。IP 与密钥使用当前用户 DPAPI 加密记忆，“忘记连接”清除选中的连接。连接后切档约三秒自动重连，连续切换采用最后档位。

## 限制与安全

v0.1.0 仅单控制端、主显示器、IPv4。尚无音频、文件传输、录屏、剪贴板同步、自动发现和移动端。Windows 输入法的 Unicode 组合文本不传输；物理按键可由 Mac 自身输入法处理。

共享正常登录后的桌面，不创建新登录会话，不绕过 FileVault 预启动，不保证未登录时控制登录界面。锁屏和安全输入框取决于系统权限与行为，不能保证；休眠或合盖后可能无法连接。

Mac 用 Keychain 保存密钥，Windows 用 DPAPI；传输使用 HMAC 认证及 AES-GCM 加密。协议未经独立审计，没有前向保密。密钥等同控制权限，只交给可信设备，不公开端口。详情见 [协议](docs/zh-CN/protocol.md)。

Mac 临时签名更新后可能导致权限或钥匙串信任失效。固定有效 Apple 签名身份可以减少身份变化，但不能承诺系统永不询问。见 [Mac 安装与排障](docs/zh-CN/macos.md)。

## 后续方向与验证

最终希望各设备安装对应版本，通过统一的设备列表、配对和连接界面，发起或接受控制。先完成 Windows ↔ Mac，再评估手机控制端及其他桌面平台；这些是计划，当前尚未支持。始终以局域网为产品重点。见 [路线图](docs/zh-CN/roadmap.md)。

Windows 已实际构建，真实 WPF/FFmpeg 客户端通过模拟加密连接测试；用户反馈实际 Windows→Mac 已可使用。本次发版环境是 Windows，无法独立编译或运行最终 Mac 应用；新增 Mac 名称与图标资源尚未实机验证，所以不发布未经验证的 Mac 二进制。见 [验证记录](docs/zh-CN/validation.md)。

为减少迁移影响，暂时保留内部 RemoteMacLAN 工程/程序名、`/Applications/RemoteMacLAN.app`、`com.remotemaclan.host`、RML1 协议及凭据位置；产品及仓库名是 LocalRemote。

[架构](docs/zh-CN/architecture.md) · [参与维护](CONTRIBUTING.md) · [发版流程](docs/zh-CN/releasing.md)。仓库仍私有，尚未选择开源许可证；第三方说明见 [NOTICE](NOTICE.md)。
