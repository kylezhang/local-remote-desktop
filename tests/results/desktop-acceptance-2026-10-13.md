# Desktop acceptance progress — 2026-10-13

Version: 0.3.0-alpha.2. Native interoperability began from source `49e919a`.
Windows: x64, .NET SDK 8.0.425. Mac: Apple Silicon, macOS 27.0.1,
Swift 6.4 / SDK 27, installed internal build 27. This is progress evidence,
not a release approval. No device addresses, secrets or captured work are included.

| Scope | Result | Evidence / boundary |
|---|---|---|
| Background physical-network interoperability | Pass | Native Mac ControllerSession/VideoToolbox ↔ production WindowsHost: generated H.264 at three qualities, ordered recorded input/release, Unicode recorded text in both directions, three regular-file fixtures both ways (Unicode filename, empty file, deterministic 512 KiB binary). No real pasteboard or OS input. |
| Installed Mac screen → Windows | Partial pass | Real ScreenCaptureKit/hardware H.264 decoded by the Windows client; at least three live frames. Native capabilities reported Accessibility disabled, so the guarded probe submitted no input. Owner later reported connection/control working; this is an owner observation, not completion of the fixture matrix. |
| Mac → Windows native input | Fail / partial | Synthetic MF hardware video decoded natively on Mac; the dedicated Windows field received letter and top-row digit as text. Click/wheel acceptance did not pass. Earlier click count included the manual focus click and is not remote-input evidence; the helper now resets counters after focus. |
| Owner pointer report | Open | Owner reported Mac clicks landing on the Windows taskbar. No foreground test was restarted after the owner stopped the acceptance period. |
| Background pointer/DPI fix checks | Pass | Production physical display enumeration is identical under unaware, system-aware and per-monitor contexts. A recording native boundary verifies per-monitor context and restoration (success/failure). Single, left, above and mixed-scale portrait layouts preserve move/click/wheel capture positions. This does not establish actual OS click acceptance. |
| Windows backend regression after fix | Pass | Framing/authentication, isolated DPAPI, host lifecycle/cleanup, input rejection/release and generated-video MF hardware encode/encrypted loopback/decode at all three qualities. No desktop capture, user clipboard or OS input. |
| Mac distribution upgrade signing | Not passed | No valid maintained developer signing identity was available; current build is ad-hoc. Two signed updates and notarized distribution remain outstanding. |
| Real clipboard/files, duration and latency | Not run | Background recorded adapters/file fixtures are insufficient to clear everyday clipboard or sustained-session gates. |

## Pointer correction

The old implementation queried physical capture/virtual bounds in a temporary
per-monitor DPI context, then submitted input on async workers after restoring
their default context. Read-only native probes demonstrated different virtual
desktop bounds between system-aware and per-monitor contexts on a mixed-scale
two-display setup. Display modes now supply physical coordinates independently
of thread awareness; capture/input share a snapshot, and every synchronous native
submission uses/restores the same per-monitor context. Physical landing after
this correction still requires the next owner-agreed acceptance period.

Reference: [Microsoft EnumDisplaySettingsEx physical-pixel contract](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumdisplaysettingsexw)
and [MOUSEINPUT absolute/virtual desktop coordinates](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-mouseinput).

## Next gate

Use the verified Windows package when the current client is idle. In the next
agreed period, confirm pointer positions/click/drag/wheel at corners and center,
then finish effective Mac permissions, native clipboard/files and duration checks.
Do not create a new tag/release on the strength of background checks alone.

中文概述：后台真实局域网的原生解码、记录型输入/文字以及普通文件样例互通通过；
真实 Windows 测试框只确认了字母和主键盘数字，鼠标/滚轮未通过，人工聚焦点击不计入验收。
用户停止集中验收后没有重启前台测试。已修复物理坐标与工作线程 DPI 上下文不一致，
后台回归通过；实际鼠标落点、系统剪贴板/文件、长时间性能及稳定签名更新仍待验收。
