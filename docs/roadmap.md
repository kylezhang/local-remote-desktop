# Roadmap

[简体中文](zh-CN/roadmap.md) · [README](../README.md)

LocalRemote aims to provide one familiar LAN remote-control application per platform. Each desktop installation will eventually initiate or accept control, with explicit authorization. Discovery never implies trust.

Only v0.1.0 is released. The versions below are targets, not completed features or delivery-date promises. Unreleased main-branch improvements will be packaged in the next release after validation.

Track v0.1.1 implementation and remaining validation in the [release checklist](next-release.md).

The v0.2.0 optional text clipboard prototype is now implemented on main with
portable/Windows encrypted-peer tests. Native Mac acceptance remains pending;
this does not change the published release or declare the v0.2.0 feature set complete.

## Version plan

| Version | Focus | Planned scope | Release gate |
|---|---|---|---|
| v0.1.0 — released | First usable product | Windows → Apple Silicon Mac, paired/encrypted current-session control, remembered connection, full screen, reconnect, quality selection and diagnostics | Published baseline; see validation notes below |
| v0.1.1 — next | Installation and update reliability | Confirmed blueberry identity, icon packaging, clear permission actions, clean replacement/preserve-permissions options; improve signing and Keychain update behavior | Native Mac build and install/update tests; Windows package validated; signing/authorization behavior documented |
| v0.1.2 | Input and responsiveness | Keyboard layouts and numeric keypad, focus/modifier release, disconnect cleanup, sleep/wake recovery; measure and reduce input/video delay | Repeatable real two-device tests, no stuck modifiers, measured latency baseline and verified recovery |
| v0.2.0 | Easier daily use | LAN device discovery, explicit pairing/revocation, optional bounded text clipboard sync, English UI with Chinese localization | Discovery cannot bypass pairing; clipboard is opt-in with size limits; both languages tested |
| v0.3.0 | Desktop control in both directions | Windows capture/input host, Mac viewer/controller, one app per desktop with Connect and Allow connections modes | Windows ↔ Mac verified on real hardware; authorization, disconnect release and session continuity pass |
| v1.0.0 | Stable desktop release | Consolidate desktop features, reliable installers and upgrade paths, onboarding, supported-system matrix and regression coverage | Both desktop directions meet documented release criteria; native builds and signed distribution validated |
| v1.1.0 | Mobile controller | Android controller first: connect to Windows/Mac, touch, text entry and small-screen navigation | Real phone-to-desktop tests and explicit authorization; mobile receiving control is outside this scope |
| v1.2.0 | Broader mobile access | iOS/iPadOS controller, subject to platform validation; tablet interaction refinements | Feasibility and distribution requirements verified before committing the release scope |
| v1.3.0 | Linux desktop | Linux controller and host, interoperating with Windows/Mac; define supported desktop sessions and packaging | Capture/input permissions and both control directions verified on supported real Linux systems |
| v1.4.0 | HarmonyOS controller | Native phone/tablet controller for supported Windows/Mac/Linux hosts; adapt touch, text entry and pairing | Native SDK, permissions, distribution and real-device interoperability verified; receiving control is outside this scope |

Linux and HarmonyOS are versioned targets, not current support claims. HarmonyOS desktop control requires a separate feasibility assessment; the v1.4.0 target covers phone/tablet controllers only. File transfer, multiple displays and audio are not assigned versions yet.

## Verification and prerequisites

The v0.1.0 Windows build and simulated-peer tests were validated; the user reported the Windows-to-Mac workflow usable. This does not substitute for an independently reproduced native Mac test matrix. See [build and test status](validation.md).

v0.1.1 must distinguish stable signed distribution from ad-hoc development builds. Stable signing needs an appropriate signing identity, and broader macOS distribution may need notarization. Changes to privacy permissions and Keychain behavior must be tested with the actual installed signature. The installer can reset only this app's decisions; it cannot automatically grant permissions or promise removal of visible Settings rows.

A goal for routine signed updates is to avoid unnecessary permission resets or repeated Keychain prompts. First-run authorization and macOS-required reauthorization remain possible. Hardware decode/render changes are justified by v0.1.2 measurements rather than bundled into a rewrite.

## Version and release rules

- Patch releases (v0.1.1, v0.1.2) improve the existing Windows → Mac workflow without adding a platform or knowingly breaking protocol/storage compatibility.
- During v0.x, new capability groups use a minor version (v0.2.0, v0.3.0); any incompatible change must be called out and tested with a migration path.
- From v1.0 onward: patches fix defects, minor versions add compatible features, and major versions carry incompatible changes.
- Keep app versions, changelogs, AtomGit tags, release notes and artifact names aligned. Mac internal bundle build numbers may advance independently.
- Never move an existing release tag or overwrite published artifacts. Label planned work and unreleased main changes accurately.
- If a release gate fails, retain the target version and fix or explicitly revise its scope; do not claim completion. See the [release process](releasing.md).

## Boundaries

LAN-first: no accounts, public traversal/relay or central cloud service. Share the current logged-in desktop, not a separate remote session. Do not promise FileVault preboot bypass or universal lock/login-screen control. Mobile support initially means controlling an authorized desktop; it does not imply unattended control of the phone itself. Security and reliability take priority over platform count.
