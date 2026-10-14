# Automated desktop release — 2026-10-14

Published version: **v0.3.0-alpha.3**, Mac internal build **28**.
Exact package/tag source: `19b0a78aaf685f12a08feca503552d48e67a06be`.
Release-automation hardening: `eb9710dbc4b84bba6a01683a4222b14511d0d1c0`.
Later workflow/document commits do not retarget this tag or replace its files.

Downloads: [AtomGit](https://atomgit.com/zkxw2008/local-remote-desktop/releases/tag/v0.3.0-alpha.3)
and [GitHub](https://github.com/kylezhang/local-remote-desktop/releases/tag/v0.3.0-alpha.3).

## Recorded evidence

| Run | Result | Scope |
|---|---|---|
| [38508092037](https://github.com/kylezhang/local-remote-desktop/actions/runs/38508092037) | Pass | Source/package checks, native Windows build and exact packaged client checks, native Mac Swift fixtures/unshown layouts/full SDK compilation, sealed ZIP/DMG and GitHub publication. |
| [38518952344](https://github.com/kylezhang/local-remote-desktop/actions/runs/38518952344) | Pass | Initial automated AtomGit API synchronization: six downloaded attachments verified, zero replacements, `release_status=pre`. |
| [38532091999](https://github.com/kylezhang/local-remote-desktop/actions/runs/38532091999) | Pass | Hardened synchronization, 22 isolated API regression cases, both native builds, six existing attachments verified and homepage status changed to `latest`. |

The hardening cases cover complete/unique checksum inventories, all GitHub API
digests, version/source/hash provenance, corruption, partial retry, checking every
existing file before uploads, conflict rejection, token-free diagnostics and
homepage selection independent of GitHub's prerelease flag.

The final job resolved the published tag to `19b0a78…`, although its own checkout
was `eb9710d…`. It reported **uploaded 0, already verified 6**. Read-back confirmed
AtomGit title `v0.3.0-alpha.3`, `release_status=latest`, the same source commit and
all six attachments. GitHub remained published with `prerelease=true`; AtomGit's
`prerelease=false` is a consequence of its homepage latest selection, not a stable
v0.3.0 certification. Source mirroring was not changed.

## Download integrity

The original GitHub downloads matched API digests, the archive checksum file and
source provenance. Files were uploaded unchanged through MCP, then independently
downloaded/hashed through the authenticated AtomGit API by Actions. A direct
unauthenticated AtomGit download returned HTTP 403 in the development environment;
the authenticated CI read-back is the recorded cross-site evidence.

| Attachment | SHA-256 |
|---|---|
| Windows x64 ZIP | `1278c18bb5864e268c47e611e6b922e3700ebabf653bbf3a20c4719555171904` |
| Mac arm64 ZIP | `796b603552062ef32fea28d356bd156152df8c04a32747c0072482b26742a2e5` |
| Mac arm64 DMG | `d2b2571d9b7bf5fb4aaa80256d46aca785f2ed479d0615365473d4f321b2476b` |
| Corresponding source ZIP | `4b66fc9e8638a0668bc962774d2e51361c386503ce37092564331e9ae391c1cc` |
| release.json | `ea8c7a1e9d2b17fffaa5c1c0652d858eae160704cf63ee54ebdfaf9044b2712d` |
| SHA256SUMS.txt | `b35410543ecae0f62cabb859f33ad086cc34c3c6f68f64a3b7e4e1267c25a600` |

## Acceptance boundaries

- Earlier Windows → Mac use and alpha.2 Mac → Windows control were reported usable
  by the owner. These are user reports for earlier checkpoints, not independent
  acceptance of every alpha.3 feature.
- Interactive tests were explicitly deferred for this release. No new physical
  desktop/input, system screen selection, TCC/Keychain upgrade, two-device
  clipboard/files or sustained hardware-performance acceptance is claimed.
- Mac is ad-hoc signed and unnotarized. CI does not grant Accessibility, bypass
  FileVault/secure desktops or establish permanently prompt-free upgrades.
- The release process did not install/open apps, inject input or reset personal
  permissions. Future physical checks need an agreed acceptance period.

中文：alpha.3 两端原生编译、后台检查、打包与自动发版均通过。工作流从已发布 tag 获取
准确源码提交，核对六个已有附件后将 AtomGit 主页设为最新；没有重新上传或覆盖旧包。
GitHub 保持预发布，AtomGit 的 latest 不代表功能已稳定。此前两方向的可用反馈属于旧检查点；
本轮按用户要求暂停实机测试，新的 Mac 权限/升级、双机剪贴板与长时间性能仍待验收。
