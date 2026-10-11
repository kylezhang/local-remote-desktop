# Desktop acceptance — v0.3.0

[简体中文](zh-CN/desktop-acceptance.md) · [Delivery plan](v0.3.0-plan.md)

Prepared on 2026-10-11. This checklist consolidates real Windows ↔ Mac acceptance;
it is not a list of results already achieved. Automated checks use isolated peers
and recording services. Run native interaction only in an agreed test period,
with a dedicated target and non-sensitive fixture content.

## Prepare once

The maintainer builds the exact committed Windows package and current Mac source
with the Apple SDK, records commit/OS/build/signature, and fixes build failures
before asking for interaction testing. Mac SDK/UI/permissions for the current
source remain pending. Do not retry the stopped SSH route.

Use a reachable private route (LAN or an existing VPN). Start sharing on the target;
TCP 47980 carries desktop/control and TCP 47981 carries regular files. Discovery
may not cross a VPN; manual Target IP is a valid acceptance path. Use an isolated
pairing password and disposable text/files, never publish real credentials, IPs,
clipboard contents or captured work in results.

## One daily-workflow pass in each direction

| Check | Required outcome |
|---|---|
| First and returning connection | Correct target name/system come from its app; wrong password stays editable; remember is opt-in and returning connection needs no repeated entry. |
| Entry UI | Compact default window; empty devices have a useful manual action; discovered/remembered rows remove the empty placeholder. Share uses available width and settings content does not become a full-height empty panel. Test English/Chinese and minimum width. |
| Current session | Real target screen updates; pointer alignment/click/drag/scroll, letters, digits and numeric keypad reach the intended app. Disconnect preserves the target's work. |
| Shortcuts and escape | Copy/paste matches the target OS; leaving focus or disconnect releases held keys/buttons. Local escape/full screen do not trap input. Submitted-event counters alone are insufficient. |
| Text clipboard | After enabling, copy fresh Unicode text both ways and paste; received content does not echo or replace a newer local copy. |
| Regular-file clipboard | Copy fresh Unicode-named, empty and multi-file fixtures both ways; wait for ready status then paste, compare bytes/SHA-256. Folders/virtual files/raw image formats are not acceptance claims. |
| Transfer cancel/revoke | Cancel or disable midway; partial batch is removed, late callbacks cannot publish, desktop control continues. Re-enable requires a fresh copy. |
| Quality and reconnect | Change each quality while connected; only the latest choice wins. Brief network loss recovers; Disconnect cancels a pending retry. Changed password/invalid authentication stops retries with an editable draft. |
| Role lifecycle | Stop sharing releases input/listeners/transfer; restart accepts a fresh paired session. Navigating pages does not start a competing app or silently authorize a reverse role. |
| Wake and duration | In a normal logged-in session, wake/reconnect behaves predictably. Run a longer complete session, record CPU/memory/frame rate/latency and compare before/after disconnect; no growing queues or orphan workers. |

## Mac installation and distribution pass

1. Test first approval, denied/retried Screen Recording and Accessibility, and
   explicit Keychain access. Permission denial must expose a useful next action,
   without automatically reinstalling, replacing passwords or repeating prompts.
2. Test two successive updates using the actual maintained signing identity.
   Default installation preserves decisions after compatibility checks. Incompatible
   signing aborts before stopping/replacing the old app. Explicit permission repair
   resets only this app's selected decisions and requires OS reapproval.
3. Confirm pairing persistence, effective permissions, bundle/icons, login agent,
   clean replacement and failure recovery. A mocked installer is not TCC acceptance.
   Ad-hoc updates cannot establish a stable product upgrade path.
4. For a distributed Mac binary, verify Developer ID/notarization/stapling and the
   extracted final package; attach exact source, notices, provenance and checksums.

FileVault preboot, system login/secure desktop bypass and universal lock-screen
control remain outside the promise. Separate these limits from a normal logged-in
session acceptance failure.

## Record and release

Record each row as Pass / Fail / Not run, direction, source commit, OS/build and
sanitized evidence. Distinguish user observations, platform builds, native fixtures
and two-device application acceptance. Fix failures and retest affected cases.
Only then align final version/package metadata and create the immutable AtomGit
tag/release. Source checks cannot clear native or signing gates.
