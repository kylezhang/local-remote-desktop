# Windows sharing preview

[简体中文](zh-CN/windows-sharing.md) · [Roadmap](roadmap.md)

Development v0.3.0-alpha.1 adds the missing Windows sharing role. Open
**Share this device** in the same Windows application.
Starting it shares the primary display of the current logged-in Windows desktop.
Opening the page alone does not start a listener or capture.

The [Mac controller source preview](mac-controller.md) is added in alpha.2, with
portable transport/state checks; native Mac acceptance remains pending.
The Windows viewer can use this host through a manually entered Target IP;
this preview is not a claim that Mac → Windows is ready. The existing Windows →
Mac workflow and clipboard implementation remain available in the control role.

## Start, stop and passwords

- Start sharing, then enter this Windows computer's LAN IP and password in a
  compatible controller. Both installations must be reachable over TCP 47980.
  Allow the application on Windows Firewall's private network profile if needed;
  the app does not modify firewall policy.
- The first password is 12 random characters. View/Copy does not change sharing.
  Change Password accepts 8–64 printable ASCII characters without spaces.
  Generation only fills a draft. Cancel leaves the current password unchanged.
- Save stores the new password atomically with current-user DPAPI, stops the old
  host and releases its held input. Start sharing again with the new password.
  A failed save preserves the existing password/session. The sharing credential
  is separate from remembered passwords for computers you control.
- Stop sharing or close the main application to close the listener and active
  session. Switching pages keeps sharing active; the app awaits cleanup on exit. Sharing is not automatically
  enabled at login or relaunch. Control and sharing roles are exclusive in this
  initial desktop interface.

## Capture and input

The first adapter uses FFmpeg GDI desktop capture and **Media Foundation hardware
H.264** (`h264_mf`), through the existing external FFmpeg dependency. It streams to
memory/pipes without recording files. Smooth targets up to 720p/30; Standard/High
up to 1080p/60 with different bitrates. Small displays are not enlarged. These are
configuration targets, not measured sustained performance guarantees.

If the FFmpeg build or hardware lacks this encoder, stop sharing and enable
Compatibility encoding to use `libx264` with two threads. Software encoding uses
more CPU. Capture errors close the controller and retain a visible retry action.
The `IDesktopCapture` adapter allows later DXGI/native MFT work without replacing
the authentication, input or interface. Hardware encoding through FFmpeg does not
mean the complete capture/render path is implemented directly in D3D.

The host advertises `platform=windows`, `inputModel=win-vk-v1` before video.
Keys use Windows VK identities with explicit modifier down/up edges; flags do
not silently convert Control into Command. Windows controllers retain native
Ctrl shortcuts for a Windows target, while preserving their Mac shortcut option.
Pointers use normalized primary-display coordinates converted to the full virtual
desktop for `SendInput`. A wheel packet can retain the current cursor position.
Malformed input is rejected; held keys/buttons are released on disconnect or
failure before waiting for encoder shutdown.

## Validation and boundaries

Tests cover independent RMP1/RML1 authentication/framing, DPAPI persistence,
unauthorized capture denial, one-controller enforcement, malformed input, fault
cleanup, actual WPF sharing/password flow and Windows-target shortcut routing.
Actual Windows desktop → hardware H.264 → encrypted loopback → decoder passes.
See [the record](../tests/results/windows-sharing-2026-10-10.txt).

Input-state tests use a recording adapter. The optional real SendInput check
targets only a test-owned window with foreground guards; this environment refused
its focus, so event acceptance remains unverified. Do not equate a successful
SendInput submission with acceptance by every application. Physical LAN, native
Mac controller and longer capture/latency acceptance remain pending.

No clipboard/files or Windows Bonjour advertisement in this sharing-role preview;
those follow role interoperability in beta.1. Primary display only. No pre-login,
lock-screen or UAC secure-desktop guarantee. Normal Windows input is constrained
by application integrity levels; the app does not elevate or bypass them.

Implementation references: [FFmpeg GDI capture](https://ffmpeg.org/ffmpeg-devices.html#gdigrab)
and [Microsoft SendInput](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput).
