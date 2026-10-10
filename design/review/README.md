# Local Remote Desktop design review

Revision 2 is **awaiting user review**. Native applications have not been updated
to this revision. See [English specification](../../docs/design.md),
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
`Blueberry482`; the local sharing password can be edited independently. Copy,
folder/settings and diagnostic actions only show simulated feedback.

Linux, Android, iOS and HarmonyOS are future design references. Mobile Share is
view-only and subject to platform consent/feasibility; these mockups do not claim
implemented support. Browser checks do not establish native runtime acceptance.

## Review images

[Overview](overview.png) · [Connect](connect.jpg) · [Settings](settings.jpg) ·
[Mac sharing](sharing.jpg) · [Remote desktop](session.jpg) ·
[Future mobile sharing](mobile-share.jpg)

These are browser captures of revision 2, not native client screenshots.
