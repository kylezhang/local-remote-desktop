# LocalRemote v0.2.0-beta.1

**Preview release: Windows package and Mac source.** Native Apple SDK build,
SwiftUI/Keychain/TCC behavior and independently reproduced two-device acceptance
are still pending. No Mac binary is attached. v0.1.0 remains the stable baseline.

## What's included

- Windows → Apple Silicon Mac current-desktop viewing and keyboard/mouse control.
- Manual 8–64 character pairing passwords or 12-character random generation;
  existing 64-character keys remain supported until explicitly replaced.
- Remembered Windows shortcuts, connection credentials, full screen and quality changes.
- Bidirectional plain-text and regular-file clipboard sharing, on by default for
  paired sessions and remembering explicit off choices. Progress, cancellation,
  encrypted transport and validation; no repeated manual pairing for files.
- Compact Windows controls and separate Mac sharing/current-password/editor flows.
- LocalRemote naming/icons, safer full-bundle replacement, explicit permissions
  actions and bilingual documentation. Stable system/credential identities retained.

## Install and update

Windows: extract `LocalRemote-v0.2.0-beta.1-windows-x64.zip` and run
`LocalRemote.exe`. The .NET Desktop Runtime and its notices are included.
FFmpeg is external: keep a complete H.264-capable installation on PATH.

Mac source, from an existing checkout:

```sh
git fetch --tags
git checkout v0.2.0-beta.1
bash apps/macos/install-app.sh
```

The installer builds automatically. The default clean update resets this app's
Screen Recording/Accessibility decisions; authorize and quit/reopen afterward.
`--keep-permissions` is only for compatible signatures and aborts otherwise.
Pairing data is retained. Ad-hoc updates can still require reauthorization or
Keychain prompts. Do not reinstall merely to reopen the app.

Both devices need the password update before switching to a short password.
On Mac, Current Password → Change Password → Save; on Windows, disconnect,
enter the new password and reconnect. Allow TCP 47980 and, for files, 47981.

## Validation and limits

The actual Windows package DLL passed WPF/FFmpeg tests against independent
encrypted peers in both legacy-key and password modes, including keyboard,
mouse, text/files, busy clipboard, revocation, quality reconnect and cleanup.
Production Swift state/model logic compiled and ran with explicit service doubles.
Windows synthetic decoder cases ran 120 seconds each at 720p/30 and 1080p/60;
both kept pace and exited after disposal. These are not a LAN latency guarantee
or an hours-long full-session soak. See [validation](validation.md).

Not included: automatic discovery, English UI, reverse control, mobile apps,
folders/image clipboard, multi-display, audio or Windows hardware decode.
One controller/display, IPv4, awake logged-in Mac. No FileVault Preboot bypass
or guaranteed login/lock-screen control. Permission/Keychain issues are not claimed
permanently fixed. Use a trusted LAN; no independent security audit or forward secrecy.

## 中文

测试版集中交付短/手填密码、文字与普通文件共享、快捷键及界面整理。
提供含 .NET 的 Windows 包与 Mac 源码，FFmpeg 沿用现有完整安装；Mac 安装脚本会自动构建。
默认清理两项本应用授权，配对数据保留；临时签名可能仍需授权和钥匙串认证。
Windows 最终包及便携 Swift 模型测试通过，Mac 原生 SDK/界面/钥匙串和真实两机验收待完成。
正式版仍为 v0.1.0；此测试版不代表完整 v0.2.0 功能已经完成，也不保证锁屏或登录前控制。
