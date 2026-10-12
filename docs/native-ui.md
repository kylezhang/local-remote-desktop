# Native interface progress

[简体中文](zh-CN/native-ui.md) · [Approved design](design.md) · [Delivery plan](v0.3.0-plan.md)

## Current implementation — 2026-10-12

The owner approved the [task-first design](entry-design.md). Both desktop sources
now implement the Start a connection / Accept connections homepage and the shared
approved display illustrations. The old permanent rail is replaced by Back and
one labeled upper-right Settings shortcut. Default frames are 560 × 568 logical
units, with work-area bounds and scrolling for long content.

Role selection navigates only. Back preserves active sharing and drafts; the
homepage keeps a View or stop route when sharing is active. Session exit returns
to connection tasks without clearing the target/password. Existing certificate
Mac login-start behavior is retained; if it starts a listener it displays Accept
connections, making Stop reachable. Ad-hoc startup never accesses Keychain.

The connection page has one list and Add target action; pairing stays in its
focused dialog. The accepting page vertically groups name, local IP and masked
password, with Start/Stop accepting connections fixed below. Existing draft,
credential-save and role-conflict guards remain. Settings remains four categories;
next-connection defaults and temporary/global consent rules are unchanged.

Windows About/diagnostics includes its assembly source identity and interface
revision `entry-2026-10-12`. Mac internal build is **24**; its signed
`source-revision.txt`, About/diagnostics and permission diagnostic script identify
the source and installed executable. A ZIP without Git says unknown, and modified
source is labeled. A failed build/signature preservation check retains the old
installed app; a source push is not proof of an installed Mac update.

## Verification scope

Windows builds and unshown production checks cover the bilingual compact shell,
role-choice navigation, no sharing-credential read at the chooser, active-listener
and draft retention, manual pairing, settings consent, reverse text/files,
quality changes, reconnect and repeated-session cleanup. Capture/input/clipboard
services are recording fixtures; synthetic H.264 validates decoded pixels.
No foreground window, physical input or real clipboard is used.

Swift syntax and portable production host/controller/startup/localization logic
are checked with explicit platform doubles. Installer, moved-cache, signing and
Mac package checks use disposable fixtures, not native Apple services. Native
Mac UI/API compilation and installed Keychain/TCC acceptance remain **pending**;
no Mac SSH is attempted because the Mac is outside this development LAN.

These checks do not establish physical input acceptance, live desktop latency or
native Mac visual fidelity. Public version remains 0.3.0-alpha.2. No new tag or
release is warranted until scheduled native/two-device and update acceptance.
Windows packaging is checked again against its final committed source before
copying into an idle usual launch directory; running clients are left untouched.

## Next acceptance

Use one consolidated Mac build/install review to check source build 24, homepage,
Back/Settings, explicit accepting, denied/granted permissions and secure password
editing. Then verify both remote-control directions, held-key release, real
clipboard/files, quality/reconnect and stop/resource cleanup. Keep existing
stable bundle/Keychain identities and use explicit permission repair only when
needed; never reset permissions as a visual-update workaround.

Earlier records remain historical evidence for their source revisions:
[shell](../tests/results/native-ui-2026-10-10.txt) and
[device names](../tests/results/device-names-2026-10-11.txt).
