# User guide

[简体中文](zh-CN/usage.md) · [README](../README.md)

## Interface labels

Published v0.2.0-beta.1 uses Chinese. Development builds offer English and Chinese
under Settings → Language. The initial choice follows your system; subsequent
choices are remembered and apply without reconnecting. These labels help match
the setup instructions to either interface:

| Label | Meaning |
|---|---|
| 开始共享 / 停止共享 | Start Sharing / Stop Sharing (Mac) |
| 当前密码 · 查看 / 隐藏 / 复制 | Current Password · View / Hide / Copy (Mac, development) |
| 修改密码… | Change Password in a separate editor (Mac, development) |
| 自动生成 / 保存密码 | Generate / Save Password (Mac, development) |
| 设置 → 权限 | Permission Settings (Mac) |
| 连接 / 断开 | Connect / Disconnect (Windows) |
| 忘记此连接 | Forget Connection (Windows) |
| 流畅 / 标准 / 高清 | Smooth / Standard / High |
| 复制诊断 | Copy Diagnostics (Windows) |
| Windows 快捷键 | Windows Shortcuts (development update) |
| 设置 → 剪贴板 → 同步文字 / 同步文件 | Text / File Sharing (both apps, development update) |

## Interface organization

Development desktop builds use Connect, Share this device and Settings in one
window. Connecting hides navigation/credentials and leaves a compact session bar
for quality, full screen, options and disconnect. Performance and transfer details
appear only when needed. Both sharing pages keep passwords masked and offer an
explicit edit/cancel flow. Viewing/copying a password does not start sharing.
The approved second UI revision is being applied incrementally; see
[native progress](native-ui.md) for features and validation still pending.

## This device’s name (development)

In Share this device, choose **Change name** beside **This device’s name**. The
default is the system name. Save an app-only name of 1–32 Unicode characters;
Cancel preserves the old name and **Use system name** fills the draft for review.
Saving does not rename the OS computer, change pairing or end sharing.

While sharing, Mac advertises this name on the LAN. A paired target also returns
its name/system over the existing connection; changing its name updates the
session label through heartbeat. A new manual IP has no name/system until the
target reports them. Older targets keep the known name or IP. Windows retains
authenticated records in memory during refresh; persistent device history and
reverse discovery remain pending.

## Connect and control

Start sharing on the awake, logged-in Mac. On Windows, choose its nearby entry or
enter its IPv4 address and pairing password, select Smooth, then Connect. Legacy
64-character keys remain supported. The first address default may be the Windows
computer's own LAN address to make the subnet easier to edit; a saved target takes
precedence. Discovery selection fills the address without connecting automatically.

Click the picture for keyboard focus. In the development update, **Windows Shortcuts** is enabled by default: Ctrl maps to Mac Command, Win to Control, Alt to Option and Shift to Shift. Ctrl+C/V/X/A/Z/S therefore sends the corresponding Command shortcut. Ctrl+Shift combinations retain Shift. Changing this mode releases held remote keys; the choice is remembered on Windows. Disable it to use the original mapping (Ctrl→Control, Win→Command), including Control+C for a terminal interrupt. Published v0.1.0 packages use the original mapping.

These shortcuts operate the Mac's own clipboard; cross-device text sharing uses the separate setting below, enabled by default in current development builds. Windows-reserved shortcuts such as Alt+Tab or Win combinations may stay local. Windows input-method composition is not transmitted; the Mac's own input method can process physical keys.

F11 toggles full screen. Ctrl+Alt+Esc releases remote input and restores local focus. Clicking a local button also moves keyboard focus away from the picture. Stop Sharing or quit the Mac application to end sharing. Closing its window alone does not quit the background application.

## Pairing password (development)

Update both apps from main. On Mac, click **修改密码…** (Change Password) beside
**当前密码** (Current Password). In the editor, type a new password or click
**自动生成** (Generate), then **保存密码** (Save).
Generating only fills the edit field; it takes effect when saved. Manual passwords
accept 8–64 English letters, digits or printable symbols, without spaces, and are
case-sensitive. Generated passwords use 12 random uppercase letters/digits,
avoiding 0, 1, I and O. Prefer generation over a common word or numeric PIN.

Saving replaces the existing Keychain item and disconnects current controllers.
If sharing was running, the Host restarts it with the new password. On Windows,
disconnect to reveal the connection fields, enter the same password and reconnect.
Windows remembers it using current-user DPAPI. Restarting or reinstalling the Mac
app does not intentionally change the saved password. Cancel clears the unsaved draft without changing the password or session. Empty,
invalid or unchanged passwords cannot be saved. Failed saves keep the current
session/password and retain the draft with an inline error. Editing clears that
error; saving cannot be dismissed halfway through. Stop/quit during persistence
does not restart sharing. macOS may request Keychain authorization.

Existing 64-character keys keep working until you explicitly save a password.
The new Windows client supports both formats. An older Windows client cannot
connect to a Mac using the new password mode. Published v0.1.0 assets still use
legacy keys. Native Mac Keychain update/relaunch acceptance remains pending.

## Text clipboard sync (development)

Update both devices from main. Text sharing starts enabled unless previously
disabled. Both devices remember the choice across app launches. Adjust it in
**设置 → 剪贴板 → 同步文字** (Settings → Clipboard → Sync Text). Wait for status
**已启用** in that menu, then copy text again. Quality changes/reconnects retain
the selection. Older peers keep desktop control working and report sync
unsupported. If Mac has sharing disabled, Windows waits for Mac consent.

- Copy plain text in a Windows application, focus the remote picture, then Ctrl+V
  with Windows Shortcuts enabled. The fresh clipboard update is queued before
  the Mac paste key on the same encrypted stream.
- Copy in the remote Mac with Ctrl+C, or locally with Command+C. Allow about
  half a second plus network delay, then paste in a local Windows application.
- Disable either option or disconnect to stop sync and discard pending retries.
  Already delivered text remains in the system clipboard; in-flight data cannot
  be recalled.

Only new plain text is sent, including Unicode/emoji and line breaks. The limit
is **32 KiB UTF-8**, not a fixed character count. Oversized text and embedded NUL
characters are skipped, never truncated. Files, images and rich formatting are
excluded; received text replaces clipboard formats. Existing contents are not sent
on enable/reconnect. Copied passwords and other sensitive text are included when
opted in; use a trusted paired device.

Change detection runs every 500 ms, suppresses echo and retries busy writes for
at most two seconds. A newer local copy cancels a pending remote write. Simultaneous
copies can race; this is not clipboard history. Avoid immediately pasting after
a remote copy or while the clipboard is busy. Local Remote Desktop does not log or store
text clipboard contents. Windows UI/transport and portable Swift state/adapter tests
pass; native AppKit and real two-device paste acceptance remain pending.

## Picture quality

| Setting | Requested maximum | Requested bitrate |
|---|---|---|
| Smooth | 1280×720 at 30 fps | 4 Mbps |
| Standard | 1920×1080 at 60 fps | 10 Mbps |
| High | 1920×1080 at 60 fps | 18 Mbps |

These are requested limits, not a measured performance guarantee. Start with Smooth if interaction feels slow. Windows currently uses software video decoding.

Selecting quality during a connection releases held input, closes the previous stream and automatically reconnects after about three seconds. Rapid selections use the latest setting. Quality changes work even if Auto Reconnect is disabled. Normal network-recovery reconnect requires a previously authenticated session and Auto Reconnect enabled.

## Connection and permission help

- Allow TCP 47980 to the Mac, plus 47981 for optional file sharing. Guest Wi-Fi isolation or VPN routing may prevent devices from reaching each other.
- Grant Screen Recording and Accessibility to the installed Mac application. After granting, quit/reopen rather than reinstall. Without Accessibility, viewing may work while control does not. See [Mac troubleshooting](macos.md).
- Pairing keys are stored in Mac Keychain and Windows current-user DPAPI. Forget Connection clears the selected saved Windows connection. Never include the key in an issue, screenshot or public message.
- Copy Diagnostics reports focus and input counters without the pairing key or typed content. KeyboardFocus=false after clicking the diagnostic button is expected. HostPosted means submitted to CGEvent, not accepted by the destination application.

## Session support

One controller, the primary display and IPv4. Published v0.1.0 has no clipboard
sync; development text sync is described above, and optional regular-file sharing
is documented in the [file guide](files.md). No audio, recording or automatic discovery yet. The Mac must be awake and logged in. No FileVault
Preboot bypass, new login session or guaranteed secure-field/lock-screen input.

The protocol is encrypted and authenticated but has not had an independent security audit and does not provide forward secrecy. Use a trusted LAN and do not expose the port to the internet. See [protocol details](protocol.md) and [build/test status](validation.md).
