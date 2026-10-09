# v0.1.1 release checklist

[简体中文](zh-CN/next-release.md) · [Roadmap](roadmap.md)

Status on 2026-10-09: development on main. **Not released.** Public app versions
remain 0.1.0 until release preparation; Mac internal build number is 8.
Existing v0.1.0 tag and assets remain unchanged.

## Implemented and locally checked

- [x] Confirmed vector identity and platform icon exports; Windows built/rendered.
- [x] Installer validates staged signature and icons before replacement.
- [x] Complete replacement without merging old resources; keep/reset-permissions
  choices remain scoped to this app.
- [x] Bundle rollback on late installation failure or interrupt/termination;
  backup retained and reported if recovery fails. Verified in disposable mocks.
- [x] Launch failure keeps the verified new bundle and gives recovery guidance.
- [x] Keychain access moved off the UI thread onto a serial queue, silent read
  on startup, explicit authentication/creation and process-memory reuse.
  Implementation and syntax checked only; native behavior is pending.
- [x] English/Chinese documentation, hygiene and release sanity checks.

## Required before publishing v0.1.1

- [ ] Actual Apple SDK compilation of the updated Host, including deprecated
  file-based Keychain compatibility APIs.
- [ ] Real Mac first launch, silent read with trusted/locked/changed-signature
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

There is no native Mac build or independently reproduced Keychain/TCC validation
in the current Windows environment. Do not describe permissions as permanently
fixed or system password prompts as eliminated. Installer rollback restores
bundle files, not prior privacy decisions, running processes or power-loss state.
