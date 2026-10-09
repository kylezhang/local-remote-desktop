# User guide

[简体中文](zh-CN/usage.md) · [README](../README.md)

## Interface labels

The current UI is Chinese. English localization is planned. These labels help match the setup instructions to the application:

| Label | Meaning |
|---|---|
| 开始共享 / 停止共享 | Start Sharing / Stop Sharing (Mac) |
| 显示配对密钥 | Show Pairing Key (Mac) |
| 复制配对密钥 | Copy Pairing Key (Mac) |
| 打开权限设置 | Open Permission Settings (Mac) |
| 连接 / 断开 | Connect / Disconnect (Windows) |
| 忘记连接 | Forget Connection (Windows) |
| 流畅 / 标准 / 高清 | Smooth / Standard / High |
| 复制诊断 | Copy Diagnostics (Windows) |
| Windows 快捷键 | Windows Shortcuts (development update) |

## Connect and control

Start sharing on the awake, logged-in Mac. On Windows, enter the Mac's IPv4 address and 64-character hexadecimal pairing key, select Smooth, then Connect. The first address default may be the Windows computer's own LAN address to make the subnet easier to edit; it does not discover the Mac. A previously saved Mac address/key takes precedence.

Click the picture for keyboard focus. In the development update, **Windows Shortcuts** is enabled by default: Ctrl maps to Mac Command, Win to Control, Alt to Option and Shift to Shift. Ctrl+C/V/X/A/Z/S therefore sends the corresponding Command shortcut. Ctrl+Shift combinations retain Shift. Changing this mode releases held remote keys; the choice is remembered on Windows. Disable it to use the original mapping (Ctrl→Control, Win→Command), including Control+C for a terminal interrupt. Published v0.1.0 packages use the original mapping.

These shortcuts operate the Mac's own clipboard. Copying text in a local Windows application does not yet transfer that text to the Mac. Windows-reserved shortcuts such as Alt+Tab or Win combinations may stay local. Windows input-method composition is not transmitted; the Mac's own input method can process physical keys.

F11 toggles full screen. Ctrl+Alt+Esc releases remote input and restores local focus. Clicking a local button also moves keyboard focus away from the picture. Stop Sharing or quit the Mac application to end sharing. Closing its window alone does not quit the background application.

## Picture quality

| Setting | Requested maximum | Requested bitrate |
|---|---|---|
| Smooth | 1280×720 at 30 fps | 4 Mbps |
| Standard | 1920×1080 at 60 fps | 10 Mbps |
| High | 1920×1080 at 60 fps | 18 Mbps |

These are requested limits, not a measured performance guarantee. Start with Smooth if interaction feels slow. Windows currently uses software video decoding.

Selecting quality during a connection releases held input, closes the previous stream and automatically reconnects after about three seconds. Rapid selections use the latest setting. Quality changes work even if Auto Reconnect is disabled. Normal network-recovery reconnect requires a previously authenticated session and Auto Reconnect enabled.

## Connection and permission help

- Allow TCP 47980 to the Mac. Guest Wi-Fi isolation or VPN routing may prevent devices from reaching each other.
- Grant Screen Recording and Accessibility to the installed Mac application. After granting, quit/reopen rather than reinstall. Without Accessibility, viewing may work while control does not. See [Mac troubleshooting](macos.md).
- Pairing keys are stored in Mac Keychain and Windows current-user DPAPI. Forget Connection clears the selected saved Windows connection. Never include the key in an issue, screenshot or public message.
- Copy Diagnostics reports focus and input counters without the pairing key or typed content. KeyboardFocus=false after clicking the diagnostic button is expected. HostPosted means submitted to CGEvent, not accepted by the destination application.

## Session support

One controller, the primary display and IPv4. No clipboard sync, file transfer, audio, recording or automatic discovery yet. The Mac must be awake and logged in. No FileVault Preboot bypass, new login session or guaranteed secure-field/lock-screen input.

The protocol is encrypted and authenticated but has not had an independent security audit and does not provide forward secrecy. Use a trusted LAN and do not expose the port to the internet. See [protocol details](protocol.md) and [build/test status](validation.md).
