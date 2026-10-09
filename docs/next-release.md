# v0.1.1 release checklist

[简体中文](zh-CN/next-release.md) · [Roadmap](roadmap.md)

Status on 2026-10-09: development on main. **Not released.** Public app versions
remain 0.1.0 until release preparation; Mac internal build number is 12.
Existing v0.1.0 tag and assets remain unchanged.

## Implemented and locally checked

- [x] Bounded Windows decoder diagnostics, idempotent disposal and direct cleanup
  on connected window close; disconnected display timer stops. Production decoder
  passed short resource checks; exact rebuilt DLL passed WPF regression. Full-session
  allocation/CPU optimization and long runs remain pending; see [performance](performance.md).

- [x] LocalRemote source/project/artifact naming and platform layout; Windows build,
  encrypted storage migration and mock-peer regression passed. Mac legacy bundle,
  signing-pin and login-path migrations passed mocks, with native verification pending.

- [x] Confirmed vector identity and platform icon exports; Windows built/rendered.
- [x] Installer validates staged signature and icons before replacement.
- [x] Complete replacement without merging old resources; keep/reset-permissions
  choices remain scoped to this app.
- [x] Bundle rollback on late installation failure or interrupt/termination;
  backup retained and reported if recovery fails. Verified in disposable mocks.
- [x] Launch failure keeps the verified new bundle and gives recovery guidance.
- [x] Preserve-permissions updates check equal designated requirements and
  verify both bundles before stopping the old Host or touching permissions.
  Incompatible/unreadable/invalid signatures fail safely in mock scenarios.
- [x] Keychain access moved off the UI thread onto a serial queue. Ad-hoc or
  unmarked builds defer every credential call until a user action. Certificate
  builds use scoped legacy interaction control on the explicitly file-based store.
  Explicit authentication/creation and process-memory reuse remain.
  Implementation and syntax checked only; native behavior is pending.
- [x] English/Chinese documentation, hygiene and release sanity checks.
- [x] Screen access checked before credentials; standalone key viewing preserved.
  Production ordering/cache/failure/cancellation logic compiled and executed with
  Swift 5.9.2 on Ubuntu. Apple framework/system-dialog checks remain pending.
- [x] Remembered Windows shortcut mode converts Ctrl→Command and Win→Control,
  releases held keys on change and retains native mapping. Actual WPF shortcut
  chords passed through the encrypted mock peer. Real Mac application acceptance
  is pending; cross-device clipboard synchronization is not added.

## Required before publishing v0.1.1

- [ ] Run the Mac candidate pipeline with the real distribution identity/profile:
  signed, Accepted notarization, stapled ticket, final ZIP extraction and assessment.
  The pipeline and portable inventory/provenance checks are implemented and mocked,
  not natively exercised. See [maintainer packaging](macos-signing.md#maintainer-packaging).

- [ ] Actual Apple SDK compilation of the updated Host, including deprecated
  file-based Keychain compatibility APIs.
- [ ] Real Mac first launch (zero startup credential calls in development),
  certificate-build silent read with trusted/locked/changed-signature
  Keychain states, denial/cancellation, retry, restart and unchanged pairing key.
  No UI hang, automatic retry loop or secret-bearing diagnostic output.
- [ ] Real Mac replacement, preserve-permissions update, clean reset update,
  installed icon, login agent and recovery behavior.
- [ ] Existing valid signing identity and stable-signature update tests;
  document ad-hoc limitations. No signing credentials are committed.
- [ ] Windows → Mac ordinary session, view/control permissions and reconnect
  regression against the final Host. Lock/FileVault boundaries remain unchanged.
- [ ] Align public versions, titles and changelogs; build/test final Windows
  package, licenses and checksums; tag exact release commit and create AtomGit
  Release only after these checks pass.

User feedback on 2026-10-09 reports repeated two-password groups and needing
to remove both permission entries manually after a preserve-permissions update.
This is an unresolved acceptance failure, not a successful permissions test.
Installer guards passed mocks; the shared startup helper now runs with a real
Swift compiler. Neither proves native Keychain/TCC behavior. See the
[signing and distribution plan](macos-signing.md).

There is no native Mac build or independently reproduced Keychain/TCC validation
in the current Windows environment. Do not describe permissions as permanently
fixed or system password prompts as eliminated. Installer rollback restores
bundle files, not prior privacy decisions, running processes or power-loss state.
