# Interface design

[简体中文](zh-CN/design.md) · [Product direction](product.md)

**Approved on 2026-10-12.** The [task-first design](entry-design.md) replaces
revision 2 as the current desktop reference. Open the
[interactive design](../design/entry-review/index.html) or its
[three-screen overview](../design/entry-review/overview-zh.png).

The homepage offers two equal choices: **Start a connection** and **Accept
connections**. There is no subtitle, permanent side rail or credential form.
The paired display illustrations use blue to identify the operated screen;
they are task illustrations, not additional application logos.

Selecting a role opens its page. It does not connect, grant permission, rotate
passwords or stop a listener. Back returns home; an active listener retains a
visible View or stop route. Settings has one labeled shortcut in the upper right.
A remote session keeps its own compact toolbar and returns to connection tasks
when disconnected rather than imposing another role choice.

Start a connection uses one device list and one Add target action. Credentials
open in a focused dialog/sheet. Names and systems come from discovery,
authenticated peer metadata or saved records; an unknown IP stays an IP.
Discovery never authorizes access, and credentials are saved only after successful
authentication. Failed pairing and Cancel retain the established safeguards.

Accept connections vertically groups the local name, OS-supplied IP and masked
password. Start/Stop accepting connections stays at the bottom. An unsaved
name/password draft blocks Start, but never blocks active Stop. Generate fills
an eight-character draft, Cancel changes nothing, and successful Save ends the
old sharing session and waits for explicit Start. Existing longer credentials
remain valid until deliberately replaced. Mac screen/input permission recovery
belongs here; role navigation itself never requests those permissions.

The default frame is approximately 600 × 680 logical units, including native
chrome. Use native typography and window controls, consistent spacing, white
content, neutral surfaces and #5265EC accents. Long content scrolls without
hiding Stop. Windows and Mac reuse the same approved illustration assets.

Settings remains General, Connection, Clipboard and About. Defaults affect new
logical sessions; temporary session options cannot exceed global clipboard/file
consent. Existing encrypted transport and credential identities are retained.

The preview contains fictional data and simulated interactions. It is not a
native screenshot or evidence of runtime acceptance. See [native progress](native-ui.md)
for build/check evidence. The older [revision 2](../design/review/README.md) is
archived design history; future mobile/Linux screens remain references, not
availability claims.
