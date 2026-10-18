# Published release and development-source checkpoint

Read-only API inspection on 2026-10-18. This record updates download guidance;
it does not publish a version, replace an asset, change mirroring or clear any
physical acceptance gate.

## Published alpha.5

[GitHub v0.3.0-alpha.5](https://github.com/kylezhang/local-remote-desktop/releases/tag/v0.3.0-alpha.5)
records source `fb1e8d1a4e61bb9605365a814d634139a30bac47`.
[Run 38554671377](https://github.com/kylezhang/local-remote-desktop/actions/runs/38554671377)
reports success for Source and packaging checks, Windows x64 and macOS Apple
Silicon. Its publication job is canceled; that run is not described as entirely
successful. The release presently exists with these six assets:

- LocalRemoteDesktop-v0.3.0-alpha.5-windows-x64.zip
- LocalRemoteDesktop-v0.3.0-alpha.5-macos-arm64-preview.zip
- LocalRemoteDesktop-v0.3.0-alpha.5-macos-arm64-preview.dmg
- LocalRemoteDesktop-v0.3.0-alpha.5-source.zip
- SHA256SUMS.txt
- release.json

The [AtomGit alpha.5 release](https://atomgit.com/zkxw2008/local-remote-desktop/releases/tag/v0.3.0-alpha.5)
lists only the Mac ZIP/DMG and explicit corresponding-source ZIP as uploaded
attachments. The Windows ZIP, SHA256SUMS.txt and release.json are absent from that
attachment inventory. Auto-generated source archives are different entries, not
substitutes for missing binary/provenance files. This is an inventory check, not
a new byte-for-byte verification of the existing release assets.

Earlier CI logs reported a timeout uploading the Windows ZIP to AtomGit. The
existing mirror and publication configuration are unchanged by this inspection.
README download guidance points to the complete GitHub set until asset parity
has been re-established. Historical release assets and notes remain immutable.

## Newer development source

The screen-desk Home/task/session source is newer than the published alpha.5
packages. Batches 1–2 have their own native CI/render evidence. Session source
`e198e0a` and documentation follow-up `1db7b13` are on AtomGit; the latest
GitHub source observed in this inspection remains `506c0c8`.
Native verification for the session batch is pending automatic synchronization.
Old CI results cannot validate that newer application code.

Alpha.6 is a planned candidate, not a new assigned app version or a published
release. Exact-source native builds, session renders, package checks and the
documented acceptance decision remain required. No working client is upgraded.

中文：只读核对确认 alpha.5 已发布，包源码为 fb1e8d1；对应源码/打包及两端原生任务通过，
但该次工作流发行任务被取消，不能声称整个工作流成功。GitHub 有六个发行附件，
AtomGit 当前只有 Mac ZIP/DMG 与明确源码 ZIP，缺 Windows ZIP、SHA256SUMS.txt、release.json。
这里只核对附件清单，未重新验证两站已有文件的字节一致性。下载说明改为使用完整的 GitHub
附件集，未修改镜像、覆盖历史附件或发新版。主分支屏幕连接台是后续开发内容，不在
alpha.5 下载中；会话批次仍等待自动同步和对应原生验证，alpha.6 仅为计划候选。
