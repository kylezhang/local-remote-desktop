# Interface conventions

[简体中文](zh-CN/interface.md)

Local Remote Desktop uses one product vocabulary and familiar workflows across platforms,
with native controls, typography, permissions and keyboard conventions. These
rules apply to the v0.2.0 interface work and v0.3.0 desktop roles; planned roles
must not appear as usable actions before implementation.

## Structure and actions

- **Connect**: choose a nearby device or enter its **Target IP**, enter **Pairing
  password**, then connect. Discovery fills the address; it never pairs or starts
  control automatically. Remembered connections stay convenient.
- **Share this device**: start/stop sharing the current desktop and manage its
  password. Keep the local stop action reachable. Sharing is explicitly enabled.
- **Settings**: General, Connection, Clipboard and About. Connection defaults
  affect new logical connections; current-session options are temporary. Clipboard
  settings grant local consent and session options can only further restrict it.
  Keep sharing permissions/passwords on Share and target credentials on Connect.
- During control, prioritize the remote screen. Use one compact bar for target,
  picture quality, disconnect/full screen and settings. Hide credential editing
  during a session. Transfers use a temporary progress/cancel notice, not another
  permanent toolbar. Mobile places equivalent actions in a compact sheet.

Connection states have the same meanings: Not connected, Searching, Connecting,
Connected, Reconnecting and Failed. Sharing has separate Off, Starting, Available
and In use states; changing a password never masquerades as starting a connection.
Errors name the failed action and a useful next action. Pairing/password validation
is inline; system permission prompts retain their native presentation.

## Appearance and interaction

Desktop entry windows start at approximately 640 × 620 logical units including
native chrome, centered inside the available screen with a margin. Fit smaller
work areas; keep settings/forms sized to their content rather than increasing
empty space. Navigation does not reset a user’s resized window. Long content
scrolls, and the remote desktop supports resize/fullscreen. Mobile layouts use
native safe areas and available space, not a fixed desktop window. See the
[development handbook](development-handbook.md) for the recorded agreements.

Keep the approved blueberry icon and existing palette during this implementation:
accent #5265EC, text #202435, secondary #697183, white content, rail #F6F7FA and
rules #E5E8F0; use readable system/dark equivalents. Do not add a second
logo to a desktop title bar. Use Segoe UI on Windows and system San Francisco on
Mac; future Linux/mobile apps use their native system typography.

Use common content hierarchy and spacing, left-aligned controls and short labels.
Allow translated text to resize naturally without crowding the remote screen.
Respect native window chrome, keyboard focus, accessibility names, reduced motion
and touch target sizes. Avoid decorative glow or motion around ordinary controls.

Shortcuts follow the selected target: Windows Ctrl+C/V on Mac maps to Command+C/V;
the reverse direction should offer equivalent Command-to-Control behavior. Keep
an explicit native mapping option, local escape from control and held-key release
on focus loss/disconnect. OS-reserved combinations and secure desktops have their
own limits; consistent workflow cannot remove system restrictions.

## Acceptance

Check English and Chinese strings, minimum desktop width, keyboard navigation,
visible focus, password editing/cancel/error states, device selection without
automatic trust, transfer progress, full screen and disconnect recovery on each
native platform. A desktop screenshot or mocked platform check cannot stand in
for another OS or phone/tablet acceptance.

## Language

Development builds offer Settings → Language → English / 简体中文 on both
desktops. The initial choice follows the system language; an explicit choice is
remembered and updates the open interface without disconnecting. Both apps use
the same versioned catalog in `assets/localization/strings.json`.

Translate controls and application messages, never pairing passwords, device
names, file paths or clipboard contents. Native dialogs and underlying OS error
details retain their system language. Windows uses a compact settings icon and
full-screen icon with localized tooltips/accessibility names; Mac uses its native
menu. Equivalent actions may have different native presentations.

## Design review

[Revision 2](design.md) reorganizes settings and sharpens page/state/visual rules.
It is approved for incremental native implementation. The settings structure
is General / Connection / Clipboard / About, with
sharing permissions and credentials owned by their task pages. See the
[implementation contract](../design/review/revision-2.md) before implementing.

[Product direction](product.md) and the [interactive UI proposal](design.md) define
the next interface iteration. The desktop proposal was reviewed on 2026-10-10. Navigation and session layouts
are being applied incrementally; see [native UI progress](native-ui.md) for
verified coverage and remaining work. Future platform views remain references.

The Connect page is one device list with a single **Add device** entry. Selecting
or refreshing a list must never authenticate automatically. Explicitly activating
a device opens the connection dialog, prefilled with its real address and metadata;
Add device opens the same form for manual IP entry. Keep password, remember,
forget and picture quality in that dialog, with errors retaining the draft.
An empty list explains how to discover or add a target without duplicating actions.

Place Start sharing / Stop sharing in a fixed bottom action area on the Share
page, after device information and pairing password in the task flow. The content
scrolls independently so Stop stays reachable when editing or in a small window.

Share groups device information and pairing password vertically in one
content-sized surface. Settings fit the available width and their actual
content height. Preserve window size across navigation. Windows software encoding
is an optional troubleshooting fallback, disabled during sharing; system limitations
belong in documentation, not a permanent compatibility panel.
