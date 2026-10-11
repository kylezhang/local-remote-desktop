# Local Remote Desktop design review

Revision 2 is **approved for incremental implementation**. Local-name editing and
authenticated peer names are in native source; the rest of the revision is not yet
fully applied. See [English specification](../../docs/design.md),
[中文说明](../../docs/zh-CN/design.md) and the [implementation contract](revision-2.md).

Open `index.html` in a browser, or serve this repository locally and open
`design/review/index.html`. The preview consists of HTML/CSS/JavaScript plus the
approved [brand SVG](../../assets/brand/icon.svg); it uses no external fonts,
services or packages. All devices, credentials, desktop content and files are
fictional. No network connection, clipboard access, persistent storage, permission
request or file transfer occurs.

Use the outside preview toolbar to select a platform/language/page. The native
app inside has Connect, Share this device and Settings. Settings has General,
Connection, Clipboard and About tabs. Session options have a distinct scope from
defaults and cannot override disabled global clipboard permission.

Preview scenarios cover first/remembered/manual pairing, wrong password, no
devices, offline saved devices, role conflict, sharing/input permissions, active
sharing, temporary transfer and reconnect. The fictional target password is
`Blue482X`; the local sharing password can be edited independently. Copy,
folder/settings and diagnostic actions only show simulated feedback.

Linux, Android, iOS and HarmonyOS are future design references. Mobile Share is
view-only and subject to platform consent/feasibility; these mockups do not claim
implemented support. Browser checks do not establish native runtime acceptance.

## Review images

[Overview](overview.png) · [Connect](connect.jpg) · [Settings](settings.jpg) ·
[Mac sharing](sharing.jpg) · [Remote desktop](session.jpg) ·
[Future mobile sharing](mobile-share.jpg)

These are browser captures of revision 2, not native client screenshots.

Manual-IP fixtures: `192.168.1.24` matches the nearby Studio Mac (macOS).
`192.168.1.70` is initially undiscovered: its name and Windows platform appear
only after simulated authentication. Other IPs remain IP / unknown system;
the preview never guesses a platform. Generated and newly saved passwords are
exactly eight characters. These fixtures are not usable real-device credentials.

Latest review details: [manual target information](manual-address.jpg),
[password editor](password-edit.jpg), and
[device added after simulated authentication](manual-connected.jpg).

Share → This device’s name → Change name demonstrates local display-name editing.
Default names are fictional OS fixtures. Edits last only for the current preview;
no OS settings, disk storage or real LAN announcements are changed. Cancel and
Use system name operate on the draft; saving retains sharing and the password.
See the [name editor](local-name.jpg).
