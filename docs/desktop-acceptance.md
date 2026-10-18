# Desktop acceptance — v0.3.0

[简体中文](zh-CN/desktop-acceptance.md) · [Ordered plan](v0.3.0-plan.md)

Updated **2026-10-18**. This is a remaining physical-test procedure, not achieved
results. Interactive tests are **paused**; prepare fixtures/evidence without
operating either working computer.

## Evidence and preparation

Alpha.3 native CI, isolated fixtures, unshown layouts, packages and automatic
release synchronization passed: [release record](../tests/results/automated-release-2026-10-14.md).
Earlier owner reports cover both directions, not every alpha.3 feature.
The [previous controlled pass](../tests/results/desktop-acceptance-2026-10-13.md)
failed pointer/wheel acceptance; DPI correction needs physical retesting.
The system picker/installed upgrade flow is still unverified.

Before involving the owner, select an exact successful CI candidate, record
source/version/Mac build/OS/SDK/signature, and fix build/fixture failures.
Local compilation is optional. Prepare dedicated input and disposable text/files.
Do not use personal clipboards or publish credentials, addresses or captured work.
Agree duration/devices and stop when the owner resumes work; schedule sustained
performance separately.

Use a reachable LAN/existing VPN. Target TCP 47980 carries desktop/control,
47981 files. Discovery may not cross VPN; manual IP is a valid acceptance path.

## P0: input and Mac access first

Input cases run in **Windows → Mac and Mac → Windows** on the same candidate.

| ID | Required outcome |
|---|---|
| I1 | Real current screen; correct pointer at actual DPI/scale, resize and fullscreen. Click/drag/wheel hit the dedicated app, not the taskbar. |
| I2 | Letters, top-row digits, keypad and target-native copy/paste work in the configured mapping. OS-reserved combinations remain documented limits. |
| I3 | Focus loss, local escape, disconnect and Stop release held keys/buttons. Submitted counters do not prove application acceptance. |
| M1 | macOS 14+ main-display picker succeeds; cancel/deny/retry has a useful next action, with no listener or repeated Keychain access before selection. macOS 13 uses global Screen Recording. |
| M2 | Accessibility is independent; denial does not claim control is available. After normal OS approval, real input reaches the dedicated app. |
| M3 | Pairing read/save/deny/retry/relaunch preserves credentials/drafts without unrequested rotation or prompt loops. |
| M4 | Two actual successive ad-hoc package upgrades record prompts/recovery and retain pairing/accepting function. Incompatible local-script signing stops before replacement; downloaded-package upgrades are tested separately. |

Picker permission is capture-only. Relaunch requires a fresh selection; same-run
reconnect/quality reuse the source. Follow [screen access](mac-screen-access.md),
including macOS 14–15.1 single-display restrictions. One cloud SDK/OS does not
establish all supported-system behavior.

## P1: daily workflow, each direction

| ID | Required outcome |
|---|---|
| D1 | Real peer name/OS; first/remembered pairing, editable wrong-password draft, opt-in remember. Discovery/refresh never authorize or connect automatically. |
| U1 | Approved task-first entry, approximately 600 × 680 default clamped to work area; one Add device, clear empty/nearby/recent states, vertical Accept content and bottom Start/Stop. Both languages, focus, minimum width and routine fit; retain user resizing. |
| U2 | During control, Home → Settings → Return to session retains the same connection and picture without another password prompt. Keys/buttons release when leaving the canvas; typing in Settings stays local. A reconnect in the background does not steal focus or change pages. Disconnect while in Settings removes Return to session and keeps Settings visible. Competing roles remain unavailable. |
| U3 | Waiting/reconnecting states show actual connection status until a real frame is presented. View-only feedback matches the target's independent input approval; it never implies permission has been granted. Session actions remain reachable at supported minimum size and fullscreen has a clear local exit. |
| C1 | Fresh Unicode text pastes both ways; no echo or replacement of a newer local copy. |
| C2 | Temporary session text/file choices cannot override a disabled global setting. Turning sync off during a transfer immediately cancels work and clears busy progress; old replies do not restore syncing. Re-enable establishes a fresh copy baseline without reconnecting the desktop or changing future connection defaults. |
| F1 | Fresh Unicode-named, empty and multiple files paste both ways after ready status; exact bytes/SHA-256. Folders/virtual files/raw image formats are outside current support. |
| F2 | Cancel/disable cleans partial batches and stale callbacks; desktop control continues; re-enable needs fresh copy. |
| R1 | All connected qualities work, rapid changes settle on latest. Transient reconnect succeeds; Disconnect cancels retry; changed credentials/invalid authentication stop retry with editable draft. |
| L1 | Stop clears input/listeners/transfers; Start accepts fresh session. Navigation preserves role/drafts without another app or reciprocal control. Disconnect leaves target work intact. |
| L2 | Normal logged-in wake/reconnect; installed icons, optional login agent, replacement and documented failure recovery work. |
| R2 | Repeatable real frame-rate/input-delay baseline at three qualities; agreed sustained session (initial target at least 30 minutes), CPU/memory and reconnect/transfer observations; no accumulating workers/queues or continued work after disconnect. |

State hardware/direction/quality/method. Synthetic decode throughput is not LAN
input delay. The first sustained pass is a baseline, not hours-long leak-free proof.

## P2: delivery and recording

Verify exact downloaded ZIP/DMG, source/provenance, GPL/notices, versions and all
checksums. Test the installed app, not an older source copy or Downloads.
Ad-hoc/unnotarized Mac distribution is supported; disclose first-open and possible
Accessibility/Keychain reapproval. Developer ID/notarization is optional.
Do not disable Gatekeeper, auto-grant access or use repeat reinstall/reset as recovery.

Record each ID **Pass / Fail / Not run**, direction, exact package/source,
OS/build, sanitized evidence and retest. Failed paths stay open until retested.

- Beta recommendation: P0 and basic installed workflow pass both ways; list remaining P1.
- Stable v0.3.0: full required matrix and final packages pass.
- Alpha.3's deferred-test exception does not automatically apply to another preview.
- Homepage latest and CI success do not clear physical gates.

FileVault/pre-login/secure-desktop bypass and universal lock-screen control remain
outside the promise. Updating this checklist does not resume real input/clipboard/
permission tests.
