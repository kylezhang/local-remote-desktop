# Changelog

[简体中文](CHANGELOG.zh-CN.md)

## Unreleased

- Redesigned the icon around desktop/mobile devices and remote interaction.
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
