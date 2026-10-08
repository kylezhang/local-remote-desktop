# Roadmap

[简体中文](zh-CN/roadmap.md) · [README](../README.md)

LocalRemote aims to become a single familiar LAN remote-control application on each supported platform. An installation may initiate or accept control; accepting requires explicit authorization. Discovery never implies trust. This is a direction, not a delivery-date promise.

## v0.1.0 — first product release

Windows→Apple Silicon Mac current-session control, paired/encrypted transport, remembered connection, full screen, reconnect, connected quality selection and Mac permission diagnostics. Product name/icon, bilingual documentation, cleaned native codebase and versioned release artifacts. Earlier development version numbers are superseded without rewriting Git history.

## Next — reliable daily use

Prioritize reproducible macOS native build/testing, stable signing and packaging, TCC/Keychain update behavior, keyboard layouts and release-on-disconnect, sleep/wake recovery, and measured input/video latency. Add an English interface with Chinese localization, keeping labels consistent with the English-first documentation, before broader international distribution. Replace FFmpeg/WPF with hardware decode/render only when evidence supports the change. Add device discovery and an explicit pairing/revocation interface after the manual-IP path is stable. Evaluate bounded, opt-in text clipboard synchronization. Exit criterion: repeatable two-device validation, documented update behavior and no reliance on repeated permission resets.

## Then — Windows ↔ Mac

Implement Windows desktop capture/input reception and a Mac viewing/control UI. Reuse the tested protocol boundaries where appropriate. Present “Connect to a device” and “Allow connections” in one application per platform; retain separation of privileges internally. Exit criterion: both directions verified on real hardware, including authorization, disconnect release and local-session continuity.

## Later — mobile controllers and other desktops

Start mobile support as a controller for authorized desktop sessions, adapting touch, keyboard entry and small-screen navigation. Evaluate Linux based on desktop-session permissions and available test hardware. Do not assume that iOS/Android can accept unattended remote control; platform constraints require separate assessment.

## Boundaries

LAN-first. No accounts, public traversal/relay, central cloud service or FileVault bypass. The current release does not add file transfer, multiple displays, audio, recording, camera or organization administration. Reconsider additions only when they serve the focused workflow and can be tested. Security and reliability take priority over claiming broad platform support.
