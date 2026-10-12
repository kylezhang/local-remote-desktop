# Task-first entry proposal

[简体中文](zh-CN/entry-design.md) · [Previous UI proposal](design.md)

Recorded 2026-10-12. The owner agreed to explore two initial entrances. These
three screens are **a design draft for review**, not approved native changes.
Open the [interactive draft](../design/entry-review/index.html) or see the
[three-screen overview](../design/entry-review/overview-zh.png).

## Structure

First launch presents two equally clear choices: **Connect to a device** (operate another
device) and **Allow connections** (Chinese: **允许连接**, let another device operate
this one). Choosing a role opens its task screen; it never starts a connection
or enables sharing. One application still provides both roles. No permanent
sidebar or credential form on the first screen.

| Screen | Content and primary action |
|---|---|
| Home | Two large role buttons; a secondary Settings entry. |
| Connect to a device | One device list, refresh and one Add target button. Credentials appear only in the target/pairing dialog. Empty state explains what to do. |
| Allow connections | Vertically stacked local name, IP and masked password. Allow connections is the page's bottom action after those details. |

Back returns to the role chooser without discarding credentials, saved history
or an active sharing session. It is navigation, not Disconnect or Stop. An
actual connection while sharing needs the existing explicit Stop accepting connections &
connect confirmation after target validation; Cancel keeps sharing. Switching
the displayed page must not silently change a role's active session.

The proposal specifically addresses **first launch**. Returning launch behavior
is not decided by this mockup: avoid imposing another mandatory choice on every
routine use. Retain an active role and keep Stop/Disconnect reachable; remembering
a last-used role may be assessed during review. The preview intentionally resets
on reload and stores nothing.

## Vocabulary refinement

The owner asked to avoid mixing a passive “被连接” entrance with “sharing”
actions. Proposed visible terms now form one connection vocabulary:

| Context | English | Chinese |
|---|---|---|
| Operate another device | Connect to a device | 连接设备 |
| Receive access to this device | Allow connections | 允许连接 |
| Open the local listener | Allow connections | 允许连接 |
| Stop the local listener/session | Stop accepting connections | 停止接收连接 |
| Establish/end a remote session | Connect / Disconnect | 连接 / 断开 |

This does not remove pairing or change protocol authorization. Internally the
code can retain the sharing role name; users do not need that extra term. File
clipboard settings say Sync files / 同步文件. Statuses identify not accepting,
waiting and connected states in their page context. This vocabulary is part of
the draft and still subject to review.

The homepage keeps only the main question: no explanatory subtitle or role
subtitles. Each role's icon and label are centered as one group. Icons share the
approved logo's rounded screen and opposing-arrow geometry; the first contains
a pointer, the second an incoming arrow. They are supporting symbols, not two
new application logos. Native title-bar icons retain OS conventions.

## Task details

Device names/platforms are discovery announcements, authenticated peer metadata
or saved records, never inferred from an IP. Fictional local devices are distinct
from the fictional target list. Native discovery must exclude this device.
Unknown manual addresses remain addresses until metadata is received. First
pairing uses the existing protocol and secure credential storage; there is no
new account or authentication step in the role choice.

Local IP is supplied by the OS and copied, not edited as an app preference.
Reachable interfaces/VPN addresses need the existing native selection rules.
The password is masked by default. Change opens an eight-character draft with
an obvious Generate button; Cancel changes nothing. Save ends sharing and waits
for explicit Start. Existing longer passwords remain valid until deliberately
changed. Show/Copy and consent behavior keep the established implementation rules.

Mac permission recovery appears on Allow connections when relevant, not on the
first screen or connection-to-another-device form. Opening the screen does not
request Keychain or TCC access. Permission grants remain system-controlled.
Settings retains General, Connection, Clipboard and About; the small preview
sheet only sketches their existing scope, not a new settings specification.

## Visual contract

Use the approved icon and #5265EC accent, neutral white/grey content and native
fonts/chrome. The same hierarchy and wording apply to Windows/macOS, without
copying one OS's title bar to the other. Draft frame: 560 × 568 logical pixels,
22 × 28 content padding, two equally prominent entrances, no permanent rail.
The exact native minimum and scaling remain acceptance work. Normal task content
fits the frame; additional permission text/long content scrolls with the sharing
action kept reachable. Device credentials are a focused modal, not a second pane.

The supplied PNGs are browser renders, not installed Mac/Windows screenshots.
The preview covers language, platform chrome, empty list, pairing error,
eight-character draft/generation/cancel/save, explicit sharing, missing permission
and role conflict. It runs offline with fictional data; Copy, refresh and permission
buttons provide simulated feedback. It never performs networking, clipboard or
storage/system actions.

## Mac source/startup audit

Source audited at c0582fe: `apps/macos/Package.swift` builds LocalRemoteDesktop;
`App.swift` installs DesktopShell into its NSHostingView with Connect as the
initial page. HostView is embedded only in the Share page, alongside the bottom
SharingActionView. `build-app.sh` packages that SwiftPM product and
`install-app.sh` targets `/Applications/Local Remote Desktop.app` after build and
signature preflights. No old standalone-Host root was found in this source path.

The owner's actual installed/running binary is **not verified**. A failed build
or signature compatibility check preserves the old application; a running old
copy or different checkout is also possible. These are candidate explanations,
not an established cause. Public alpha.2 labels alone do not identify a source
commit. Internal build 23 is present in source, but its installed value is unknown.
Do not claim the Mac was updated based on a push or a Windows build.

Next implementation should expose non-secret source/build identity and compare
built, installed and running executable paths before concluding an update worked.
Keep stable bundle/Keychain identities and never reset permissions as a UI fix.
No Mac SSH or native validation was performed for this draft.

## Review and implementation

Confirm these three task screens before changing clients. Then implement the
shared flow incrementally, preserve encrypted transport/session behavior, and
verify native builds and actual installed launches separately from headless checks.
This is not a new release or a promise of future-platform availability.
