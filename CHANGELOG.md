# Changelog

## 0.3.0-alpha.3 — in development

- Use the macOS 14+ system display picker for deliberate accepting, pass its
  authorized filter directly to capture, and reuse it within the application run.
- Preserve denial/cancel ordering: no credential read or listener before selection.
- Provide a Mac DMG with an Applications link for a canonical installation path.
- Maintain a no-paid-membership distribution policy; signature/notarization status
  is separate from functional stability. Native permission acceptance is pending.

[简体中文](CHANGELOG.zh-CN.md)

## Unreleased

- CI now mirrors the published GitHub preview release to AtomGit: only missing
  attachments are uploaded, checksum conflicts halt the sync, and
  `release_status` is set only after full read-back verification.

- Mac builds discard stale native caches after a checkout moves or is renamed,
  preventing absolute module-cache path errors while retaining incremental builds.

- Quality changes now cancel an in-progress Windows authentication attempt.
  Fullscreen and focus transitions release remote input and clear suppressed paste;
  focus restoration is limited to the active app. Repeated F11 presses no longer
  toggle fullscreen repeatedly. Updated Mac fullscreen handling awaits native acceptance.

- New local clipboard copies cancel obsolete file transfers on both sharing roles.
  Mac rejects expired file offers before sending; canceled workers and previous
  Windows file sessions cannot replace current transfer notices.

- Sharing drafts must be saved or canceled before starting; active Stop stays
  available during editing. Both desktops now end sharing after a successful
  password change and wait for explicit Start. Failed saves preserve the current
  password/session; stopping clears stale transfer notices.


- Sharing uses a fixed bottom start/stop action after device information and
  pairing password. Content scrolls independently on both desktop sources.


- Desktop entry windows now start at 640 × 620. Connect has one device list and
  Add device entry, with credentials/quality shown on demand. Sharing information
  and password are stacked vertically; settings surfaces fit their content.
  Windows software encoding remains an optional troubleshooting fallback.
- Windows reconnect stops on rejected authentication or incompatible targets;
  transient network loss still retries and Disconnect cancels a pending retry.
- Normal Mac installation preserves permissions after signature checks; reset is
  explicit repair. Incompatible signatures leave the old installation untouched.
  Updated checks and a bilingual consolidated native acceptance checklist are included.

- Both desktop roles now reuse the encrypted regular-file protocol. Mac controllers
  push/pull files through a separate session-bound channel; Windows sharing adds
  consent, progress/cancel, local-copy priority and partial cleanup. Remote paste
  waits for confirmed readiness. Production encrypted-worker and unshown recording
  clipboard checks pass; updated native Mac and real two-device acceptance pending.

- Mac → Windows plain-text clipboard sync now reuses the paired encrypted desktop
  session. Both roles obey local consent, matching acknowledgments, 32 KiB limits,
  reconnect baselines and echo suppression; text queues before the paste key.
  Windows/WPF encrypted-session and portable Swift state/model/transport checks
  pass with recording clipboards. Updated native Mac pasteboard acceptance and
  real two-device clipboard acceptance remain pending.

- Development v0.3.0-alpha.2 adds the Mac controller source preview: native
  VideoToolbox decode/AppKit input, paired Windows connections, target-aware Mac
  shortcuts, full screen, latest-only presentation and bounded outgoing input.
  Quality changes wait for old-session cleanup; disconnect/close cancels retries.
  Portable production Swift core/model/wire/session checks pass. Native Apple SDK,
  Mac display/input and real two-device acceptance remain pending; no stable release
  or verified Mac binary is claimed. Mac internal build advances to 23.

- Adopt GPL-3.0-or-later for the project's original code, documentation and brand assets.
  Future binary packages include the license and matching committed source with
  build instructions; third-party licenses and existing release artifacts remain unchanged.

- Rename the product to Local Remote Desktop and its existing private AtomGit
  repository to local-remote-desktop. Update native source/assembly names, app
  titles, resources and build/package documentation. Preserve credential stores,
  Mac privacy/Keychain/login identities and discovery compatibility. Mac installers
  migrate both earlier bundle names with rollback; released assets keep their names.

- Development version advances to v0.3.0-alpha.1 with the Windows sharing role:
  opt-in native page, separate DPAPI credential, explicit password save/revocation,
  authenticated one-controller TCP, GDI capture/MF hardware H.264 with optional
  software encoding, bounded access units, native input adapter and teardown.
  Windows targets keep Control shortcuts; Mac targets retain their mapping.
  Shared Windows style resources keep the primary actions consistent. Real Windows
  desktop/encrypted-loopback/decode and WPF flow checks pass; actual OS event
  acceptance is unverified because the guarded test could not obtain focus.
  Mac controller, sharing-role clipboard/discovery and physical LAN remain pending.

- Added shared English/Chinese application strings and remembered language choice
  on both desktops. Switching updates the interface without changing credentials,
  clipboard settings or connections. Compact Windows settings/full-screen icons
  keep the English toolbar usable at minimum width. Native Mac UI acceptance is pending.

- Added Bonjour nearby-device selection to Target IP, keeping manual entry and
  pairing. Mac advertises only while sharing; Windows searches once or on refresh.
  Windows UDP/WPF and portable Mac lifecycle checks pass; native Mac LAN acceptance is pending.
- Documented shared cross-platform interface conventions and staged v0.3.0 desktop roles.

- Renamed the Windows connection field to “Target IP”, with clearer local
  IPv4 guidance. Remembered addresses and the local-IP default are preserved.
  This wording anticipates future platforms; current control remains Windows → Mac.

## 0.2.0-beta.1 — 2026-10-10

Preview checkpoint: Windows package plus Mac source. Includes the changes below since v0.1.0; native Mac acceptance and the complete v0.2.0 roadmap remain pending. Not a stable release.

- Simplified Mac sharing/password layout: fixed-width main window, hidden current
  password with explicit legacy-key label, separate change-password editor and
  inline validation/errors. Viewing credentials does not change sharing status;
  cancelled drafts and failed saves preserve existing credentials/sessions.
  Stale callbacks cannot overwrite replacement-host status, and stop/quit during
  persistence cannot restart sharing. Production model checks passed with service
  doubles; native SwiftUI/Keychain acceptance remains pending. Mac internal build 18.

- Added editable 8–64 character pairing passwords and random 12-character generation.
  Explicit save updates the existing Keychain item and disconnects old controllers;
  restarts preserve the credential and legacy keys remain until saved. RMP1 uses
  salted PBKDF2-HMAC-SHA256 and mutual proofs for desktop/file connections, retaining
  RML1 compatibility. Exact Windows WPF/password/legacy checks and portable Swift
  credential checks pass; native Mac SDK/Keychain acceptance remains pending.
  Mac internal build 17; public versions and released assets unchanged.

- Faster file sending uses elapsed-time pacing at 8 MiB/s with a bounded small-file
  allowance instead of fixed per-chunk sleeps. Progress shows percentage/rate;
  byte completion stays separate from OS clipboard readiness, and Mac-to-Windows
  completion acknowledges clipboard success after bounded UI-thread retries.
  Mac internal build 16; public release assets unchanged.

- Removed the repeated Windows toolbar logo; the native title bar retains the app icon.

- Text/file sharing now defaults on for paired sessions and remembers explicit
  off choices. Simplified Windows/Mac controls into Settings; Windows uses one compact toolbar, connected Windows
  credentials collapse, performance data is optional and file notices appear as
  needed. Mac internal build 15; public version/published packages unchanged.

- Added optional bidirectional regular-file clipboard sharing to development builds:
  automatic copy-triggered transfers on a separate encrypted TCP channel, existing
  pairing reuse plus desktop-session/consent binding, bounded chunks, SHA-256 and
  byte-count checks, progress/cancel, partial cleanup, no echo and protection for
  newer clipboard copies. File-sharing consent is remembered. Supports 1–32 files,
  4 GiB each/8 GiB total; folders and pixel-image clipboard formats are pending.
  Exact WPF/crypto-peer and portable Swift checks pass; native Mac acceptance is
  pending. Mac bundle build is 14; public version and published assets are unchanged.

- Added opt-in bidirectional plain-text clipboard sync to development builds:
  capability/mutual-consent negotiation, encrypted bounded UTF-8 transport,
  stale-request rejection, echo suppression, busy-write recovery and text-before-paste
  ordering. Defaults were initially off; the later UI update above enables new preferences. C#/Swift state tests, mocked Mac adapter
  and exact rebuilt WPF encrypted-peer tests pass; native Mac acceptance is pending.
  Published v0.1.0 assets are unchanged.

- Bounded FFmpeg diagnostic draining, made decoder disposal idempotent, terminate
  the owned decoder directly on window close, and stop disconnected display timers.
  Added real decoder resource samples and WPF close/disconnect checks. Short synthetic
  runs do not establish full-session resource use or Mac performance; frame allocation
  and software decoding remain optimization work.

- Unified source layout as apps/macos and apps/windows, Swift package/target and
  Windows project/assembly/namespace as LocalRemote, and executable files as
  LocalRemote.app/LocalRemote.exe. Updated build, test, package and bilingual docs.
  Added legacy Mac bundle/signing-pin/login-path migration and one-time encrypted
  Windows settings migration. Stable Mac authorization/Keychain/job identities and
  the RML1 wire contract remain compatible; published v0.1.0 assets are unchanged.

- Added a maintainer-only Mac candidate pipeline with Developer ID-only release signing,
  Hardened Runtime/timestamp, notarization and stapling, final ZIP extraction checks,
  provenance, notices and checksums. Portable and mocked failure tests pass; no real Mac
  candidate or notarization is claimed. Build bundles are now fresh and recover the old
  build output on replacement failure/termination; installed applications are not touched.

- Added a remembered Windows Shortcuts mode, enabled by default: Ctrl→Mac Command and Win→Control. Both modifier key codes and flags are converted, mode changes release held input, and native mapping remains available. Cross-device clipboard sync uses a separate opt-in.
- Mac sharing now checks screen access before reading credentials. Standalone pairing-key access remains available; failed/cancelled startup does not retry or proceed. The production startup helper is compiled and exercised with Swift 5.9.2 on Linux. Keychain queries explicitly retain the existing file-based backend; system dialogs remain a native acceptance item.

- Hardened Mac installation with full-bundle rollback on replacement/verification failure or interruption, retained recovery backups if rollback fails, and explicit launch-failure messages.
- Moved Keychain reads to a serial background queue; development builds now defer credential access until a sharing action, while certificate builds attempt a UI-disabled startup read. User actions allow authentication/first-time creation and loaded keys are reused for the process. Repeated system prompts remain unverified on Mac.
- Preserve-permissions installation now aborts before stopping the old Host or touching permissions if designated requirements differ, cannot be read or fail verification. This prevents changed ad-hoc updates from being presented as permission-preserving.
- Adopted the user-approved blueberry identity (#5265EC → #643AC2): white screen/arrows with a balanced interior pointer. Updated both apps' brand accents, Windows selection/checked states and Mac light/dark brand text; regenerated all platform icons from the editable SVG.
- macOS validates bundled PNG/ICNS and explicitly sets its running Dock icon, including direct LaunchAgent starts.
- Clean macOS installation now resets only this app's Screen Recording and Accessibility decisions by default; `--keep-permissions` skips resets only for compatible signatures. Added separate permission settings actions.

- Applied the selected icon to both app headers; the Windows toolbar wraps on smaller windows and remains hidden in full screen.
- Rewrote the English-first README and Chinese counterpart for users; moved detailed controls, quality settings and diagnostics to a dedicated user guide.
- Added English UI/localization to the near-term roadmap. Existing release tags and archives remain unchanged.

## 0.1.0 — 2026-10-08

First named LocalRemote product release. Earlier 0.2.x versions were development iterations; this release starts the product version at 0.1.0 without removing features or rewriting history.

- Renamed product and AtomGit repository to LocalRemote; introduced a shared SVG/PNG/ICO/ICNS icon and consistent application versions.
- Retained native Windows→Apple Silicon Mac desktop sharing/control, encrypted pairing, remembered connection, full screen, input diagnostics, reconnect and connected quality selection.
- Removed the retired view-only WebRTC experiment and consolidated outdated documents into English-first documentation with Chinese counterparts.
- Added repository hygiene, maintenance/release instructions, dependency notices and checksummed Windows packaging.
- Retained legacy internal executable names, Mac app identity/path and credential storage for compatibility.

Windows release build and simulated-peer tests are validated. User feedback indicates the real workflow is usable. The final Mac branding/resource update is not independently built or exercised in the Windows release environment. No native Mac binary is supplied. This release does not add bidirectional desktop control, discovery, clipboard synchronization, mobile support or Windows hardware decoding.
