# Task-first entry proposal

[简体中文](zh-CN/entry-design.md) · [Previous UI proposal](design.md)

Recorded 2026-10-12. **The three-screen design is approved for native implementation.**
Open the [interactive draft](../design/entry-review/index.html) or see the
[three-screen overview](../design/entry-review/overview-zh.png).

## Structure

First launch presents two equally clear choices: **Start a connection** (operate another
device) and **Accept connections** (Chinese: **接受连接**, let another device operate
this one). Choosing a role opens its task screen; it never starts a connection
or enables sharing. One application still provides both roles. No permanent
sidebar or credential form on the first screen.

| Screen | Content and primary action |
|---|---|
| Home | Two large role buttons; a secondary Settings entry. |
| Start a connection | One device list, refresh and one Add target button. Credentials appear only in the target/pairing dialog. Empty state explains what to do. |
| Accept connections | Vertically stacked local name, IP and masked password. Accept connections is the page's bottom action after those details. |

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

The owner confirmed 发起连接 / 接受连接 on 2026-10-12 after reviewing the
passive/permission wording. Visible terms now form one connection vocabulary:

| Context | English | Chinese |
|---|---|---|
| Operate another device | Start a connection | 发起连接 |
| Receive access to this device | Accept connections | 接受连接 |
| Open the local listener | Start accepting connections | 开始接受连接 |
| Stop the local listener/session | Stop accepting connections | 停止接受连接 |
| Establish/end a remote session | Connect / Disconnect | 连接 / 断开 |

This does not remove pairing or change protocol authorization. Internally the
code can retain the sharing role name; users do not need that extra term. File
clipboard settings say Sync files / 同步文件. Statuses identify not accepting,
waiting and connected states in their page context. The entrance names are agreed;
the full visual proposal was approved on 2026-10-12.

The homepage keeps only the main question: no explanatory subtitle or role
subtitles. Each role's icon and label are centered as one group. Both
illustrations keep the local screen in the foreground at lower left, with a
remote screen behind it at upper right. Brand blue now marks the operated
desktop, not the identity of the local machine. A curved connection path reverses
direction to show connection initiation, not video-data direction. The pointer
sits on the operated screen: remote for Start, local for Accept. Rounded screens
and palette echo the approved brand; these are functional illustrations, not new
application logos. Native title-bar icons retain OS conventions.

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

Mac permission recovery appears on Accept connections when relevant, not on the
first screen or connection-to-another-device form. Opening the screen does not
request Keychain or TCC access. Permission grants remain system-controlled.
Settings retains General, Connection, Clipboard and About; the small preview
sheet only sketches their existing scope, not a new settings specification.

## Visual contract

Use the approved icon and #5265EC accent, neutral white/grey content and native
fonts/chrome. The same hierarchy and wording apply to Windows/macOS, without
copying one OS's title bar to the other. Approved frame: 560 × 568 logical pixels,
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

## Homepage visual refinement — 2026-10-12

The owner found the earlier literal screen/arrow diagram visually unfinished.
The homepage now uses two layered display illustrations with a restrained
surface treatment, rounded hardware outlines and a curved connection path.
Depth is subtle; the brand blue is concentrated on the connection/pointer cues.
Neither illustration is a new logo or a platform-specific PC silhouette.

Task tiles use a quiet neutral surface instead of large white outlined boxes.
Heading/role type is 24/20px at weight 500. The role tiles are equally prominent,
with centered 176 × 130.24 illustrations and 15px image/title spacing. The app
frame stays 560 × 568; navigation, credential forms and role state rules are
unchanged. Hover/pressed/focus styles are defined distinctly, without unsolicited
motion. Duplicate homepage CSS was consolidated into a single readable section.

Browser checks observed Windows/Mac English/Chinese homepage rendering, single-line
English labels, loaded vector assets, complete content and exact icon/label
centering. Keyboard Tab focus had a visible outline. Hover/pressed CSS rules were
inspected; physical mouse interaction/native clients were not tested. Previous
narrow-browser checks apply to the earlier mockup; the updated narrow layout rules
have not been separately exercised at a forced viewport in this pass.

## Visual harmony pass — 2026-10-12

Retain the accepted illustration direction and entrance names. Tighten the
heading/tiles gap to 26px and illustration/title gap to 13px; tile separation is
18px. A quiet 18px corner and fine neutral boundary unify the two equal choices.
Settings is now a labeled secondary shortcut on the same upper-right baseline
across all three screens. Gear geometry is centered; device-list symbols use the
same rounded-display vocabulary. Task headings/dialog titles adopt the homepage's
medium weight rather than heavier competing headings. Window dimensions and all
connection/pairing/permission behavior are unchanged.

Browser review covered all three default screens in English/Chinese and
Windows/Mac preview chrome: 12 layouts fit the frame, each with one Settings
shortcut. Navigating from the homepage reached the expected task screens; the
labeled Settings shortcut opened its existing preview panel. Updated renders
are browser mockups, not native client evidence. Earlier empty/error scenario
screenshots remain earlier captures, not updated harmony-pass images.

## Finished-product visual pass — 2026-10-12

The owner asked for a visibly larger aesthetic improvement. This iteration
rebuilds the illustrations rather than reporting an unmeasurable percentage.
Each display has a precise casing, inset desktop, restrained depth and a
fictional application window. The operated desktop is lit in the existing blue
palette, with a native-like pointer; the other stays neutral. Local/remote
positions, connection direction, role names and pairing logic remain unchanged.
Blue is confined to the functional illustration instead of spreading across the
page. No real screen content, OS logos or new product logos are used.

The prompt is 26px, tiles 252px high, and illustrations 188 × 145.7px. This
increases useful visual content and reduces the earlier empty-space imbalance.
Equal roles retain equal size; hover/press responds through border/background/
shadow with no movement and a 120ms transition disabled for reduced motion.

Browser review observed four Windows/Mac × English/Chinese homepage variants:
all vector assets loaded, content fit, labels stayed single-line and image/text
centers matched within 0.01px. These are browser-layout observations, not native
GUI acceptance or evidence of a measured percentage improvement. Other task
pages retain the preceding harmony pass.

## Design responsibility

Review the complete flow before asking for owner feedback: vocabulary, device
identity, connection direction, operated-screen cues, action/confirmation states,
English/Chinese lengths, visual centering, spacing, hover/pressed/focus feedback
and a narrow viewport. Do not ask the owner to discover each alignment or state
mistake separately. Role choice navigates; it never grants control or ends an
active session. Owner review is for product/design decisions; native evidence
must be recorded separately.

## Review and implementation

The owner approved these three task screens on 2026-10-12. Implement the
shared flow incrementally, preserve encrypted transport/session behavior, and
verify native builds and actual installed launches separately from headless checks.
This is not a new release or a promise of future-platform availability.
