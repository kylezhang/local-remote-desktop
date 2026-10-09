# Changelog

[简体中文](CHANGELOG.zh-CN.md)

## Unreleased

- Hardened Mac installation with full-bundle rollback on replacement/verification failure or interruption, retained recovery backups if rollback fails, and explicit launch-failure messages.
- Moved Keychain reads to a serial background queue; startup attempts a silent read, user actions allow authentication/first-time creation, and loaded keys are reused for the process. Native Mac acceptance is pending.
- Adopted the user-approved blueberry identity (#5265EC → #643AC2): white screen/arrows with a balanced interior pointer. Updated both apps' brand accents, Windows selection/checked states and Mac light/dark brand text; regenerated all platform icons from the editable SVG.
- macOS validates bundled PNG/ICNS and explicitly sets its running Dock icon, including direct LaunchAgent starts.
- Clean macOS installation now resets only this app's Screen Recording and Accessibility decisions by default; `--keep-permissions` preserves both for routine updates. Added separate permission settings actions.

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
