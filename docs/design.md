# Interface proposal

[简体中文](zh-CN/design.md) · [Product direction](product.md) · [Delivery plan](v0.3.0-plan.md)

**Revision 2 — approved for incremental implementation.** This design responds to
feedback about settings, page logic, executable naming and native appearance.
Local display-name editing and authenticated peer metadata are now in both desktop
sources. Other interaction and visual changes remain implementation work; see
[native UI progress](native-ui.md) for the precise validation scope.

Open the [interactive proposal](../design/review/index.html). It runs offline using
fictional devices, passwords and content, with no networking, clipboard, storage
or system actions. Windows/macOS are the desktop design targets; Linux/mobile
selectors are future references. Preview controls outside the app are review
tools, not product navigation. The [implementation contract](../design/review/revision-2.md)
records state rules, setting scope, visual tokens and acceptance steps.

## The everyday flow

**Open → choose a device → connect → work.** One app provides Connect and Share
this device. Desktop navigation places those two tasks at the top, Settings at
the bottom, and disappears during remote control. Use a readable device list and
a focused selected-device pane rather than a dashboard of cards. No permanent
LAN slogans, repeated logos or credential toolbar around the remote picture.

An online remembered device connects with one action using its saved password.
A new target has an inline pairing form; a wrong password stays available to
correct. Manual Target IP and password share one dialog. Offline remembered
devices remain identifiable without claiming that they are ready. Discovery
does not authenticate a device. Forget password affects only the selected
target's saved credential, not all application preferences or the local key.

Names and platforms come from device announcements or saved records, never from
the IP itself. A manual-IP dialog shows a matching record when available;
otherwise it shows the address until connection succeeds. Authenticated peer
metadata then updates the list and session label, without duplicate rows.
The native protocol now carries the peer name after authentication and on heartbeat;
the offline preview simulates that flow. Failed pairing or Cancel adds no device.

Share this device owns the local name, IP, masked password, availability/in-use
state and visible stop action. Changing pages preserves sharing. Change password
opens a draft: Generate fills it, Cancel changes nothing, Save explains that
sharing stops and the current connection ends. Recovery from missing screen or
input permission belongs here, never in the connection-to-another-device flow.
View-only sharing is labeled when input permission is missing.

New passwords use exactly eight characters without spaces, both manually and
automatically. Generate is a clearly bordered secondary action. Existing native
passwords remain usable until explicitly changed; updates must not truncate them.

Share → This device’s name → Change name edits the app’s advertised display name.
The default comes from the local system; Use system name fills the draft, Cancel
preserves the saved value, and Save updates it without ending sharing or changing
the password. The name is the same in discovery and authenticated connection
metadata; the target app reports its platform automatically. Renaming does not
rename the OS computer, create another device record or discard saved credentials.
The native editor and authenticated name field are implemented. Mac advertisement
and Windows reception use the custom name; Windows advertisement and Mac discovery
remain follow-up work. Native saved-device history is not yet complete.

The current one-role restriction has an explicit Stop sharing & connect action
after target validation. Cancel preserves the existing sharing session; do not
silently stop it on device selection. Future concurrent roles need separate
implementation and acceptance.

## Settings with clear scope

| Category | Contents |
|---|---|
| General | Language, proposed theme selection |
| Connection | Quality, reconnect and shortcut defaults for new connections |
| Clipboard | Global local consent for text/files; received-file folder |
| About | Version, GPL license and expandable troubleshooting |

Permissions and the local sharing password stay on Share this device. Saved
target passwords are managed in Connect. Troubleshooting is secondary; developer
counters do not dominate routine screens.

The remote toolbar owns picture quality, full screen, session options and
Disconnect. Session options temporarily restrict text/files and set shortcut
mapping. They cannot override a disabled global clipboard permission. Disabling
global sharing immediately stops the corresponding active sync; both devices'
consent is required. Defaults for later connections and changes for this logical
session must remain distinct, including through quality changes and reconnects.

Reconnect dims the last frame and labels it paused; Cancel ends retries and
disconnects. Transfer notices are temporary and contain progress/cancel.
Cancelling a transfer does not disconnect the desktop.

## Appearance that can be implemented

Retain the approved blueberry icon and accent #5265EC. Use neutral text #202435,
secondary #697183, white content, a #F6F7FA navigation surface and #E5E8F0 separators.
Reserve brand gradients for the icon. Use consistent line icons rather than
unrelated glyphs, a restrained selection tint and native window chrome.

Windows uses Segoe UI Variable/Segoe UI, Apple systems their native font. Body is
14px, secondary 12px, headings 25px; spacing follows 4/8px steps. Main content has
32–36px padding; settings controls align to a stable column, not the far window
edge. A roughly 174px rail and 42px session toolbar preserve room for the task.
Small controls, selection marks and dialogs use distinct but consistent radii.
Stack list/details at narrow widths instead of clipping fields. Preserve visible
focus, translated text, reduced motion, native dark colors and OS scaling.

Mobile retains Connect/Share/Settings in bottom navigation and uses a session dock
for touchpad/direct touch, keyboard and options. Touch targets are at least 44px.
Its sharing proposal is view-only and requires system confirmation; it does not
promise full unattended input. See [mobile feasibility](mobile-sharing.md).

## Implement only after review

The proposed theme choice, remembered Mac controller credentials, reverse
discovery/clipboard/files and diagnostics redaction need implementation and
verification. A simulated working control is not evidence of platform support.

After approval, extract shared tokens/strings/states, implement native Windows and
Mac screens incrementally, and compare actual screenshots at supported sizes,
languages and scaling. Keep bundle identity, Keychain, signing and TCC unchanged
in this layout work. Normalize the Windows launcher to `LocalRemoteDesktop.exe`,
remove the local compatibility rename to `LocalRemote.exe` and update shortcuts
without deleting preferences or creating duplicate executables. Do not interrupt
an active session for deployment.

Native builds and actual two-device pairing/input/clipboard/files/reconnect checks
remain release gates. Browser visual review cannot replace those checks. No new
tag or release is implied by this revision.
