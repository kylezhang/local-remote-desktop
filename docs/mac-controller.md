# Mac controller preview

[简体中文](zh-CN/mac-controller.md) · [Windows sharing](windows-sharing.md)

Development **v0.3.0-alpha.2** adds a Mac controller to the existing application.
The source implements authenticated connection, H.264 reception/VideoToolbox
decoding, pointer and keyboard input, full screen, quality changes and reconnect.
Transport and state checks pass; **native Apple SDK compilation, AppKit interaction,
VideoToolbox rendering and real Mac → Windows acceptance are still pending**.
This is a development checkpoint, not a verified bidirectional release.

## Connect to Windows

1. In Windows, open **Settings → Allow connections (preview)** and start sharing.
   View its sharing password; this is separate from saved target passwords.
2. On the Mac, stop sharing if it is active. Open
   **Settings → Connect to device (preview)**.
3. Enter the Windows **Target IP** (LAN IPv4) and sharing password, then Connect.
   Both devices need TCP 47980 connectivity. Click the picture for keyboard focus.
4. Disconnect or close the controller window to release input and end the session.

The app remembers the last successfully connected address. The controller password
remains in memory while the Mac app runs, including when closing/reopening the
controller window; it is not stored on disk and must be entered after app exit.
This preview does not read or create an additional Keychain item.
Connecting alone does not require Mac Screen Recording or Accessibility: those
permissions are needed when this Mac shares its own desktop.

## Controls

- **Mac shortcuts** is enabled: Command maps to Windows Control for Command+C/V/A/Z;
  physical Control maps to Windows Win, Option to Alt, Shift to Shift. Disable this
  option for Command → Win and Control → Control. Switching releases held input.
- **F11** toggles full screen. **Control+Option+Escape** releases remote input.
  macOS may retain system shortcuts, including Command+Tab; these are not globally
  intercepted. Key codes currently follow common physical Mac/Windows keys;
  international layout and IME composition acceptance remain pending.
- Mouse movement, left/right/middle buttons, dragging and vertical scroll use
  the displayed picture's coordinates. Letterbox margins are excluded; drags clamp
  at the image edge.
- Smooth, Standard and High reuse Windows video profiles. Changing quality closes
  the old session, waits three seconds for host cleanup and connects once using the
  latest choice, including when automatic reconnect is disabled.
- Automatic reconnect applies after an established session fails. Initial failures,
  rejected authentication/capabilities, malformed video and input overflow require
  a deliberate retry. Disconnect, window close and disabling pending automatic
  reconnect cancel retries. The target releases held input when its socket closes.

## Implementation and validation

RMP1 and legacy RML1 mutual authentication reuse the existing encrypted record
format. The controller refuses unsupported input capabilities before sending
input. A single writer retains at most 256 pending messages and coalesces adjacent
pointer moves; overload closes the session. Release-all purges pending input.
Video records are bounded to 8 MiB; header dimensions are bounded to 4K area.
Annex B parsing converts SPS/PPS and access units for native VideoToolbox decoding.
Only one decoded image waits for presentation; old images are replaced, not queued.

[Checks and exact coverage](../tests/MacControllerChecks/README.md) use independent
Python AES-GCM peers, production Swift transport/session/model code and explicit
platform adapters. Linux uses OpenSSL adapters in place of native CryptoKit and
CoreGraphics. Passing them does not establish native decode or input acceptance.
See [validation status](validation.md) for the remaining gates.

## Boundaries

One controller, primary Windows display and the logged-in desktop. Windows UAC
secure desktop, elevated application input, lock screen and pre-login control are
not guaranteed. This reverse-role preview has no clipboard/file synchronization,
Windows discovery advertisement or persistent Mac controller password; those belong
to the following role-interoperability stage. Existing Windows → Mac clipboard and
file support remain in the original direction. Simultaneous sharing and controlling
are disabled in the interface to avoid reciprocal desktop feedback.
