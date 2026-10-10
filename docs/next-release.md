# Release checkpoint

[简体中文](zh-CN/next-release.md) · [Roadmap](roadmap.md)

2026-10-10: **v0.2.0-beta.1 preview published** on [AtomGit](https://atomgit.com/zkxw2008/LocalRemote/releases). Windows and Mac display the beta
identifier; Mac numeric bundle version is 0.2.0, internal build 19. The existing
v0.1.0 tag/assets stay unchanged. Earlier v0.1.1/v0.1.2 patch plans are consolidated
because this checkpoint also adds password and clipboard capabilities.

## Preview delivery

- [x] Windows package prepared with .NET runtime/notices and accurate setup instructions.
- [x] Packaged-DLL WPF/encrypted-peer checks in both RML1 and RMP1 modes.
- [x] Short/manual passwords, keyboard shortcuts, text/files, compact controls and Mac editor.
- [x] Portable Swift credential/startup/HostModel checks; no native SDK claim.
- [x] Synthetic Windows decoder checks, 120 seconds per quality, cleanup and samples recorded.
- [x] Mac package validation rejects beta metadata presented as a stable binary; mocks pass.
- [x] English/Chinese scope, limitations, changelog and version labels aligned.
- [x] Final build from the release commit, exact final-package regression and ZIP/checksum verification.
- [x] AtomGit annotated tag, prerelease, attached Windows ZIP/checksums and download verification.

Preview scope: tested Windows package plus **Mac source only**. See
[release notes](release-v0.2.0-beta.1.md) and [delivery verification](../tests/results/release-v0.2.0-beta.1-2026-10-10.txt). No unverified signed Mac binary is published.

## Stable v0.2.0 gates

The locally verified beta.2 checkpoint added discovery and remembered languages;
see the [language checks](../tests/results/localization-2026-10-10.txt).
Main now targets **v0.3.0-alpha.1 development**, Mac numeric version 0.3.0/build 21.
It adds a [Windows sharing preview](windows-sharing.md), not the Mac controller.
No new alpha release/tag or Mac binary is claimed. The native gates below remain.

- [ ] Native Apple SDK build and real Mac interface/password editor acceptance.
- [ ] Keychain authorization/denial/retry, explicit save, restart persistence and update behavior.
- [ ] Real installed signature, clean/preserved permissions, icon/login-agent and recovery tests.
- [ ] Windows → Mac keyboard/mouse, text/files, consent/revoke, reconnect and sleep/wake regression.
- [ ] Longer complete-session resource/latency checks; current decoder samples are not a soak.
- [ ] Discovery and English/Chinese UI, or an explicit revision of stable feature scope.
- [ ] For a Mac binary: actual Developer ID signing, accepted notarization/stapling and extracted-package checks.
- [ ] Final stable versions, packages, checksums, release notes and immutable tag.

There is currently no native Mac environment attached to this Windows workspace.
A stable release date depends on those checks; repeating local mocks cannot clear
them. Do not claim repeated Keychain prompts/TCC issues permanently fixed. A beta
is an implementation checkpoint, not evidence that these native gates passed.
