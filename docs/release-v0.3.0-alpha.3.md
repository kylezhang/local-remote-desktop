# Local Remote Desktop v0.3.0-alpha.3

**Pre-release for Windows x64 and Apple Silicon macOS.** Cloud native compilation,
background checks and package verification are required for these downloads.
The maintainer has deferred interactive two-device acceptance for this release;
the new Mac permission flow is **not yet verified on a physical desktop**.

## Included

- One native app per platform with Start a connection / Accept connections roles.
- Windows ↔ macOS current-session control implementations, remembered pairing,
  eight-character password editing/generation, quality selection and reconnect.
- Paired text/regular-file clipboard implementations, discovery and English/Chinese UI.
- macOS 14+ system display selection for accepting connections, rather than
  returning to stale global Screen Recording authorization. Canceling selection
  opens no listener and reads no credentials; cached selections are reused in the
  current app run. Relaunch requires selection again. macOS 13 uses global permission.
- Mac DMG with an Applications shortcut, ZIPs, matching GPL-3.0-or-later source,
  third-party notices, checksums and exact CI/source provenance.

Earlier versions were reported usable for Windows → Mac, and the owner reported
successful Mac → Windows control in alpha.2. These reports do not certify all
alpha.3 features or replace permission/clipboard/update acceptance.

## Installation

**Windows:** extract the Windows ZIP and run `LocalRemoteDesktop.exe`. Keep the
whole folder; .NET is included. FFmpeg is external: use a complete H.264-capable
installation on PATH. Do not run multiple copies to update a running session.

**Mac:** open the DMG, quit an older running copy, drag **Local Remote Desktop.app**
to **Applications**, and open that installed copy. Do not launch the copy in Downloads.
The app is **ad-hoc signed and not notarized**; normal first-open approval may be
needed. Do not disable Gatekeeper. On macOS 14+, choose Accept connections, start,
then select the main display. Accessibility must separately allow the current app
for keyboard/mouse input. Updates can require renewed Accessibility/Keychain approval;
the installer does not silently grant these permissions. No developer membership
or local build is required to use the downloads.

## Known limits

One primary display and controller, IPv4 direct reachability, normal logged-in
desktop sessions. No FileVault preboot, guaranteed login/lock-screen control or
secure-desktop bypass. Folder/image clipboard formats are not supported.
macOS 14–15.1 display selection currently requires a single-display setup.
Real permission/upgrade behavior, two-device clipboard/files, hardware performance
and sustained sessions remain to be accepted. This is an alpha, not stable v0.3.0.

## 中文

**Windows x64 / Apple Silicon Mac 预发布版。** 附件须通过云端原生编译、后台检查和包校验。
按维护者本轮要求暂不做互动实机验收；新的 Mac 屏幕权限流程尚未实机确认。

本版包含统一的发起/接受连接入口、双向桌面控制实现、连接记忆、8 位密码编辑/生成、
画质与重连、文字/普通文件剪贴板、设备发现及中英文界面。macOS 14 及以上开始接受连接时
使用系统选择器授权主屏幕，取消不读钥匙串、不开监听；同次运行复用选择，重开后重新选择。
Mac 提供 DMG 和 ZIP，两端附精确源码、许可、说明与校验值。

Windows 解压后运行 LocalRemoteDesktop.exe，包内已有 .NET；FFmpeg 需独立完整安装。
Mac 退出旧程序后，通过 DMG 拖入 Applications，再从应用程序打开，避免运行下载目录副本。
Mac 为临时签名、未公证，可能需首次打开确认；键鼠控制仍需当前应用的辅助功能授权，
更新后也可能重新认可辅助功能/钥匙串。不关闭 Gatekeeper，不要求购买开发者会员或本地编译。

用户曾反馈早期 Windows → Mac 和 alpha.2 Mac → Windows 可用，不等于本版全部功能验收。
当前仅主屏幕、一个控制端、IPv4 可达的正常登录会话；不承诺 FileVault、登录前、锁屏或安全桌面控制。
文件夹/图片剪贴板暂不支持；macOS 14–15.1 需单显示器。权限升级、双机剪贴板/文件及长时间性能待验证。
