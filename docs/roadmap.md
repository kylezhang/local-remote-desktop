# Roadmap

[简体中文](zh-CN/roadmap.md) · [README](../README.md)

Local Remote Desktop aims to provide one native, familiar application per
platform. Desktop apps initiate or accept authorized control of the current
session. Discovery helps find a target; it does not establish trust.

## Current milestone

The latest published preview is **v0.3.0-alpha.5**, from source **fb1e8d1**.
Its native jobs passed; the complete package/source/provenance/checksum set is
byte-identical on GitHub and AtomGit after the 2026-10-18 repair/read-back check.
See [release notes](release-v0.3.0-alpha.5.md) and the
[delivery plan](v0.3.0-plan.md). The screen-desk refresh is newer main-branch work,
not included in that download. Complete installed two-device acceptance is pending.

v0.1.0 is the historical stable baseline. v0.2.0-beta.1 delivered passwords and
clipboard improvements as a preview; that daily-use scope is carried into v0.3.0.
A separate stable v0.2.0 is not the next target. Earlier v0.1.1/v0.1.2 plans are
consolidated, not counted as completed releases.

## Version plan

| Version | Focus | Scope and gate |
|---|---|---|
| v0.1.0 — released | First usable path | Paired/encrypted Windows → Mac current-session control; earlier checks/user reports form the baseline. |
| v0.2.0-beta.1 — historical preview | Daily use | Passwords, text/regular-file clipboard and UI/input improvements; Windows package and Mac source. |
| v0.3.0-alpha.3 — published preview | Desktop roles/cloud delivery | Windows ↔ Mac, task-first UI, discovery/localization and Mac screen selection; native CI packages passed, physical acceptance pending. |
| v0.3.0-alpha.4 — historical preview | Focused corrections | Followed alpha.3; reported input/accepting failures led to the alpha.5 correction preview. |
| v0.3.0-alpha.5 — latest published preview | Permission/startup corrections | Event-posting permission, display-query fallback and specific diagnostics; native CI and cross-site asset parity passed, physical failures not certified resolved. |
| v0.3.0-alpha.6 — planned candidate | Screen-desk interface and sessions | Shared Home/task/session design, navigation without disconnect and temporary consent; source 62a1d5b passed native CI/render/package gates and has an internal acceptance candidate. Physical acceptance pending; not published. |
| v0.3.0-beta.1 — next candidate | Complete desktop flow | Clear P0 input/permission/update issues; basic installed workflow passes both ways; disclose remaining P1 cases. |
| v0.3.0 | Accepted desktop milestone | Both directions pass input, pairing, permissions, clipboard/files, quality/recovery, lifecycle and resource checks on final packages. |
| v0.3.x | Compatible corrections | Focused fixes in the accepted desktop scope, with immutable patch releases. |
| v1.0.0 | Stable desktop experience | Supported-system matrix, reliable installation/update, onboarding and sustained-use regressions; ad-hoc Mac distribution remains supported. |
| v1.1.0 | Android controller | Touch/text and small-screen control of Windows/Mac; real phone-to-desktop acceptance. |
| v1.2.0 | iOS/iPadOS controller | Native phone/tablet interaction; SDK/distribution feasibility and real-device acceptance first. |
| v1.3.0 | Linux desktop | Both roles; defined desktop sessions, capture/input permissions, packages and real interoperability. |
| v1.4.0 | HarmonyOS controller | Native phone/tablet control of supported desktops; SDK, permissions, distribution and real-device acceptance. |

Milestones are not calendar commitments; future platforms are not current
support. Mobile screen sharing/authorized receiving input need separate studies;
controller-first does not permanently exclude them. See [mobile sharing](mobile-sharing.md).
Folders, virtual-file clipboard, multiple displays and audio have no assigned versions.

## Desktop priorities

The [delivery plan](v0.3.0-plan.md) owns the ordered work list;
the [desktop checklist](desktop-acceptance.md) owns per-direction acceptance.

1. **P0:** correct input/release and predictable Mac access, credentials and actual ad-hoc updates.
2. **P1:** two-way text/files, cancel/revoke, pairing/discovery, quality/recovery and measured sustained resource behavior.
3. **P2:** final package integrity and bilingual supported-system/install/use guidance alongside fixes.

Both roles are implemented; acceptance is the current gap. Retain Windows → Mac
regressions while refining Mac → Windows. Keep native Swift/C# and the existing
authenticated LAN protocol; no broad rewrite. File services follow the accepting
session, not a fixed OS. Do not silently create reciprocal control.

Interactive tests are paused. Continue background review, isolated checks and
GitHub Actions without opening apps or changing personal permissions. Consolidate
future owner participation into an agreed period.

## Distribution and release rules

- English primary, Chinese aligned; follow [interface conventions](interface.md):
  **Start a connection / Accept connections**, native typography and shortcuts.
- Paid Apple Developer membership is not a prerequisite. Ad-hoc/unnotarized
  packages need honest first-open/update disclosures and installed acceptance;
  Developer ID/notarization is optional.
- Preserve compatibility identity/pairing and avoid incidental permission resets.
  System-required authorization remains possible.
- During v0.x use minor versions for capabilities and patches for compatible fixes.
  From v1.0 use patches for defects, minors for compatible features and majors for
  incompatible changes; document/test migrations.
- Align versions, changelog, notes, tags and filenames. Mac internal build numbers
  may advance independently. Never retarget tags or replace published assets.
- Submit source to AtomGit; its existing mirror triggers GitHub Actions. Verify
  cloud artifacts before automatic release sync. Homepage latest is not stability
  certification. See [release process](releasing.md).

Direct private routes include existing VPNs; discovery may require local multicast,
so manual IP is valid. No accounts, public traversal, cloud relay, new login
sessions, FileVault preboot or universal lock/login/secure-desktop control.
Reliability of the supported desktop flow comes before platform count.
