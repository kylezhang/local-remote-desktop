# Changelog

[简体中文](CHANGELOG.zh-CN.md)

## Unreleased

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
