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
- **Allow connections**: start/stop sharing the current desktop and manage its
  password. Keep the local stop action reachable. Sharing is explicitly enabled.
- **Settings**: the same groups in every app: Clipboard, Keyboard & connection,
  Language, then Diagnostics. Keep routine controls compact; details belong here.
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

Keep the approved blueberry icon and existing palette during this implementation:
accent #5265EC, pressed accent #4238AF, supporting accent #643AC2, text #282C67,
light surface #F7F8FF; use readable system/dark equivalents. Do not add a second
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

[Product direction](product.md) and the [interactive UI proposal](design.md) define
the next interface iteration. The proposal awaits review; it is not current app UI.
