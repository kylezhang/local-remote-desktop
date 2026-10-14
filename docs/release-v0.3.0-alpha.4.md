# Local Remote Desktop v0.3.0-alpha.4

**Pre-release for Windows x64 and Apple Silicon macOS.** The maintainer explicitly
authorized publication of this correction while interactive acceptance remains
paused. Native cloud builds, background regression and final package checks must
pass before publication. This is not stable v0.3.0.

## Changes since alpha.3

- Both controllers now preserve queued clipboard consent/revocation, text and
  file controls when focus loss, fullscreen or shortcut-mode changes release input.
  Previously the shared writer cleared these messages along with pending input,
  potentially leaving synchronization or transfer waiting indefinitely.
- Cleanup takes priority, purges only stale input and earlier releases, and keeps
  retained control payloads ordered. Repeated release stays bounded; control-only
  overload disconnects explicitly rather than silently dropping consent.
- New C#/Swift regressions reproduced the original failure and pass after the fix.
  Local Windows protocol/unshown-client and portable Swift model checks passed;
  the publication run supplies the final native CI evidence.
- Bilingual version/acceptance plans and GitHub feedback forms are included.

Existing features remain: one app per platform with Start a connection / Accept
connections, Windows ↔ Mac current-session control implementations, remembered
pairing, eight-character password editing/generation, quality/reconnect,
paired text/regular-file clipboard, discovery and English/Chinese interfaces.
macOS 14+ uses the system main-display picker; cancel opens no listener and reads
no credentials. Same-run selections are reused; relaunch selects again.

## Downloads and use

GitHub and AtomGit provide the same Windows ZIP, Mac ZIP/DMG, exact corresponding
GPL-3.0-or-later source ZIP, provenance and checksums. Published alpha.3 is retained.

**Windows:** extract the complete ZIP and run LocalRemoteDesktop.exe. .NET is
included; a complete H.264-capable FFmpeg installation remains external, on PATH
or beside the app. Quit your running copy yourself before replacing its files.

**Mac:** quit the old app, open the DMG, drag Local Remote Desktop.app into
Applications and open that installed copy. This is ad-hoc signed/unnotarized;
normal first-open or renewed Accessibility/Keychain approval may be required.
Do not disable Gatekeeper or repeatedly reinstall to reopen. On macOS 14+, start
Accept connections and select the main display. Accessibility independently
authorizes keyboard/mouse input; macOS 13 uses global Screen Recording permission.
No paid developer membership or local build is required.

## Validation boundaries

Automated checks use recording services, generated video and isolated peers.
They do not certify physical pointer acceptance, installed picker/TCC/Keychain
updates, real clipboard/files or sustained hardware performance. Earlier user
reports of both directions are not full acceptance of this release.

One primary display/controller, directly reachable private IPv4, logged-in awake
desktop. Existing VPN routes may work with manual Target IP; discovery may not
cross them. No pre-login/FileVault/secure-desktop bypass or universal lock-screen
control. Folders, virtual-file/raw image clipboard, audio and multiple displays
are outside current support. macOS 14–15.1 picker requires one display.

## 中文

**Windows x64 / Apple Silicon Mac 预发布版。** 用户明确要求发布本轮修复；交互验收继续暂停。
必须通过云端两端原生编译、后台回归及最终包核验后发布，不称为正式 v0.3.0。

修复两端丢焦点、全屏或快捷键模式变化释放输入时，误删队列中剪贴板允许/撤销、文字及文件消息。
此前这可能导致同步/传输一直等待。现在优先释放旧输入，保留控制消息顺序，重复释放有界；
全控制消息拥堵时明确断开。新回归在原 C#/Swift 失败、修复后通过；本地 Windows 协议/
不显示窗口客户端和便携 Swift 模型检查通过，发版运行附最终原生 CI 证据。
另附双语版本/验收计划及 GitHub 反馈表单。

现有统一发起/接受角色、双向桌面实现、记忆配对、8 位密码、画质/重连、文字/普通文件、
发现及双语功能保留；macOS 14+ 使用系统主屏幕选择器，同次运行复用，重开重新选择。

两站提供相同 Windows ZIP、Mac ZIP/DMG、精确 GPL 源码、来源及校验文件，不覆盖 alpha.3。
Windows 解压运行 LocalRemoteDesktop.exe，包内已有 .NET，FFmpeg 需完整外部安装。
替换运行中文件前由用户退出旧程序。Mac 退出旧应用后用 DMG 拖入 Applications，再打开安装副本；
临时签名/未公证可能需首次打开、辅助功能/钥匙串确认，不关闭 Gatekeeper 或反复重装。
键鼠仍需独立辅助功能授权，macOS 13 需全局屏幕录制；无需付费会员或本地编译。

自动测试不证明真实鼠标、选择器/TCC/钥匙串升级、双机剪贴板/文件或持续硬件性能，均待实测。
仅主屏幕、一个控制端、私有 IPv4 可达、唤醒且正常登录会话；已有 VPN 可手填 IP，发现未必跨网。
不支持登录前/FileVault/安全桌面绕过或通用锁屏控制；文件夹/虚拟文件/原始图片、音频、多显示器
尚未支持，macOS 14–15.1 需单显示器。
