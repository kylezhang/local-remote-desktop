# Native interface progress

[简体中文](zh-CN/native-ui.md) · [Approved design](design.md) · [Delivery plan](v0.3.0-plan.md)

## Current implementation — 2026-10-12

The owner approved the [task-first design](entry-design.md). Both desktop sources
now implement the Start a connection / Accept connections homepage and the shared
approved display illustrations. The old permanent rail is replaced by Back and
one labeled upper-right Settings shortcut. Default frames are 600 × 680 logical
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
revision `entry-2026-10-12`. Mac internal build is **25**; its signed
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
Mac package checks use disposable fixtures, not installed Apple services. Native
SDK compilation and unshown Mac layout checks passed on 2026-10-13 after SSH
access resumed. Installed Keychain/TCC and physical acceptance remain **pending**.

These checks do not establish physical input acceptance, live desktop latency or
native Mac visual fidelity. Public version remains 0.3.0-alpha.2. No new tag or
release is warranted until scheduled native/two-device and update acceptance.
Windows packaging is checked again against its final committed source before
copying into an idle usual launch directory; running clients are left untouched.

## Next acceptance

Use one consolidated Mac install review to check source build 27, homepage,
Back/Settings, explicit accepting, denied/granted permissions and secure password
editing. Then verify both remote-control directions, held-key release, real
clipboard/files, quality/reconnect and stop/resource cleanup. Keep existing
stable bundle/Keychain identities and use explicit permission repair only when
needed; never reset permissions as a visual-update workaround.

Earlier records remain historical evidence for their source revisions:
[shell](../tests/results/native-ui-2026-10-10.txt) and
[device names](../tests/results/device-names-2026-10-11.txt).

## Windows typography correction — 2026-10-12

The owner's installed screenshot revealed mixed CJK fonts that layout checks had
missed. Native WPF glyph-run reproduction identified JhengHei/Yu Gothic fallback
and differing actual weights. The composite mapping now explicitly selects
Microsoft YaHei UI for CJK and Segoe UI for Latin; entry labels/headings use
Regular faces. The gear is vector, and the card shadow is isolated from text.

Native glyph-run and unshown ClientChecks passed. Revision is
`entry-2026-10-12-r2`; this is a Windows-only correction, with no Mac permission,
identity or version change. Physical/installed visual acceptance remains distinct
from the font/layout checks. Final packaged assembly is rechecked before deployment.

## Routine page fit — 2026-10-12

The 560 × 568 Windows frame left the normal accepting page with a 480-unit extent
and a 397-unit viewport in the native unshown measurement. The default is now
600 × 680 (work-area constrained). Routine English/Chinese Home, device list,
Accept, four settings categories and the target dialog have no vertical scroll
at the default frame in the native layout checks. Sharing spacing is tighter,
and target Cancel/Connect remains outside the scrolling fields when errors grow.
Small work areas, long lists, editors and diagnostics retain real scrolling;
user resizing is preserved. The homepage content is vertically balanced.

Mac source adopts the same size/compact sharing structure and build 25;
interface revision is `entry-2026-10-12-r3` on both sources. Swift syntax,
portable host model and package fixtures are checked; actual Mac SDK/UI page fit
remains pending. Interactive preview dimensions are updated; stored PNGs are
older browser captures, not evidence of this native size refinement.

## Native Mac SDK validation resumed — 2026-10-13

Owner-authorized SSH access is reachable again. The complete production app was
built on Apple Silicon with macOS 27.0.1, Apple Swift 6.4 and the macOS 27 SDK,
deploying to macOS 13+. The installed Command Line Tools lacked the SwiftUIMacros
plugin used by the SDK's unqualified @State macro. A ViewState type alias explicitly
selects the existing State property wrapper for hover/settings bindings; the
minimal probe and full release build passed without installing Xcode or changing
the toolchain. Mac internal build is now 26; bundle/Keychain identities are retained.

Native VideoToolbox generated/decoded nine known-color H.264 frames through the
production ViewerDecoder, including pixel and coded/header/SPS bounds checks.
The independent Python/Swift loopback peer passed password/legacy mutual auth,
100 KiB video, ordered input, bad proof, replay, oversized records, cancellation
and session clipboard cases using native CryptoKit/CommonCrypto/Darwin sockets.
Host/controller/startup, pairing, text/files, discovery, names and 346 localization
strings passed with explicitly recording services and isolated preferences.

The full build and tests used a separate source snapshot. No GUI app was launched,
no actual desktop captured, no OS input posted and no real clipboard/Keychain
access or network/permission configuration change occurred. The build still has
existing capture Sendable and legacy Keychain deprecation warnings. Native SDK
compilation is now verified; installed UI/layout, TCC/Keychain and physical two-device
acceptance remain pending. No new tag/release is implied. See the
[native record](../tests/results/mac-native-2026-10-13.txt).

## Native layout and packaging correction — 2026-10-13

The new [Mac layout harness](../tests/MacLayoutChecks/README.md) compiled production
views/models and rendered 18 English/Chinese cases without showing a window.
Home, empty list, Accept and four settings categories fitted their inspected native
scroll containers at the default frame. Target/password dialogs rendered with
bounded fixture sizes. Review found a redundant settings picker label and a
missing English target-metadata hint; both are corrected. Direct Mac translation
keys are now checked against the shared catalog (347 strings).

Actual bundle creation exposed Bash 3.2 failures missed by a newer shell: empty
signing-option array expansion and variables adjacent to Chinese punctuation.
Signing paths now use explicit arguments and affected messages use braced
variables. The installer parses both plain and comment-prefixed codesign
requirements, retaining exact comparison and cross-verification. Changed ad-hoc
requirements still abort preservation before stopping/replacing the old app.

System Bash 3.2 passed 13 signing cases, four cache cases and 22 installer fixture
methods, including comment-prefixed requirements, failures and recovery. The real
isolated production bundle built and strictly verified with resources and internal
build 27. Development ad-hoc verification is not distribution signing acceptance.
Installed app and permissions were left untouched. Unshown inactive renders do not
verify active-window colors/focus, actual sheets, TCC/Keychain or two-device input.
See the [sanitized evidence](../tests/results/mac-layout-install-2026-10-13.txt).
