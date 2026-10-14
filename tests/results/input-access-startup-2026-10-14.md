# Input access and Windows startup — 2026-10-14

The owner reported alpha.4 showing the Mac screen without usable input, and
Windows accepting failing with the generic FFmpeg/TCP 47980 message. These
physical failures remain open; no application-acceptance pass is claimed.

## Findings and implementation

- Windows Start has not launched FFmpeg when creating geometry/listening. Its
  catch-all text was misleading. It now distinguishes occupied/denied ports,
  display-query failure and other system errors, exposes sanitized native error
  codes, restores role/action state and provides Copy sharing diagnostics.
- The development machine's read-only geometry, FFmpeg encoder list and port
  reservation check were normal. This did not reproduce the owner's actual
  exception. No process was closed, port policy changed or real desktop captured.
- DEVMODE completeness was the only Windows display path. A monitor-rectangle
  fallback now runs inside physical DPI awareness and restores caller context.
  Missing mode fields/primary mode are isolated failure fixtures, not a confirmed
  diagnosis of this specific startup report.
- Mac input used AXIsProcessTrusted for CGEvent posting. The posting operation
  now uses CGPreflightPostEventAccess, with AX recorded separately. Deliberate
  authorization uses CGRequestPostEventAccess; returning to the app refreshes
  state. Locate current app helps when macOS does not list the installed copy.
  No permission database, signature identity or stored credential is reset.
- Optional inputEnabled/eventPosting fields accompany AX in Mac capabilities
  and heartbeat. Windows prefers inputEnabled, falling back to AX for older peers.
  This does not certify a CGEvent's acceptance by another application.

## Background checks

- WindowsHostChecks passed: current native display/monitor queries agree under
  three DPI contexts; synthetic mixed-scale/negative-origin coordinates; recording
  native input; authenticated loopback, startup/disconnect/failure cleanup and
  generated video. Software encoding; no physical input or real capture.
- ClientChecks unshown suite passed, including four startup failures and retry,
  sanitized bilingual error display, new Mac posting permission precedence/legacy
  fallback, recording text/files, recovery, cleanup and native WPF layouts.
- Swift 5.9.2/Linux production HostModel and controller tests passed with explicit
  service doubles. Four AX/posting combinations, passive refresh, deliberate
  request/no-repeat and credential/listener independence were checked.
- Version/catalog/link/hygiene checks passed. Updated native Mac SDK/layout
  compilation is a cloud gate, not established by these portable checks.
- The previously authorized Mac SSH address timed out. No remote app was changed.

Still needed: the owner's input diagnostics to locate the actual lost/blocked
input stage, and the actual Windows native startup error. Installed TCC behavior,
RDP/virtual-display fallback and both physical directions remain Not run here.

References: [Apple posting preflight](https://developer.apple.com/documentation/coregraphics/cgpreflightposteventaccess()),
[posting request](https://developer.apple.com/documentation/coregraphics/cgrequestposteventaccess()),
[Microsoft monitor enumeration](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumdisplaymonitors),
[DPI contexts](https://learn.microsoft.com/en-us/windows/win32/hidpi/dpi-awareness-context).

中文：已修正 Windows 误导性的启动提示并增加物理 DPI 下的备用显示器查询，
Mac 按实际 CGEvent 权限判断并刷新，AX 单独记录。后台/隔离检查通过，
但用户这两项真实故障原因尚未完整确认，没有实机修复通过的结论。
