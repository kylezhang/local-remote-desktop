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

## Recovery completed later on 2026-10-18

The initial missing-attachment observations above are historical. The original
alpha.5 native artifacts were retrieved from run 38554671377. All four archives
matched GitHub's published API SHA-256 digests. The published release.json and
SHA256SUMS.txt were downloaded directly and also matched those digests. Locally
regenerated metadata had different serialization and was rejected before upload;
only the exact published metadata was used.

All three existing AtomGit attachments were downloaded and verified first.
Only the missing Windows ZIP, SHA256SUMS.txt and release.json were uploaded through
AtomGit-issued storage URLs. Each was downloaded again from AtomGit. All six
attachments are now byte-identical to the published GitHub digests:

| File suffix/name | SHA-256 |
|---|---|
| windows-x64.zip | 6f3c38d955535db0835ba1313cd25b9e0c1a373432711542637d5b28f79b5070 |
| macos-arm64-preview.zip | 8f044092bdbdceb8ad66c758a0568da1a2ae8adea29d782342fdef1c33fa45be |
| macos-arm64-preview.dmg | a3d9e05868f4395296339ea6794a28697d365d451eba4c44cdb014cb05268df7 |
| source.zip | 72e0619c8246509ce1f29100a71edce3dc5fb3a694fa546de291c2e552737e80 |
| release.json | add5fd3f389d0b56e5b88ef94983333b68b113140dd9b5bc24997dab1ad1581d |
| SHA256SUMS.txt | 56b8015b58f85bf09240f2ff3aaa0ce8654004e89d746dd4e783826e311d3f23 |

The canceled publication job in run 39087609228 was retried through GitHub Actions.
Attempt 2 completed successfully. Its sync job independently verified all six
existing attachments, uploaded zero files and read back release_status=latest.
AtomGit's release-by-tag and latest-release APIs both return v0.3.0-alpha.5 at the
unchanged original commit fb1e8d1. The name remains only v0.3.0-alpha.5. No old
attachment was overwritten, no new tag/release was made and mirroring was unchanged.

The newer screen-desk source 62a1d5b has now passed native CI/render/package gates
and has an isolated internal acceptance candidate. It is still separate from
the published alpha.5 at fb1e8d1. Real two-device acceptance remains paused.

Future large new uploads from a GitHub runner are not certified by this repair:
the first streaming attempt still exhausted the job limit. The successful retry
verifies the repaired inventory/idempotence/status path, not a new large-file PUT.

中文：只读核对确认 alpha.5 已发布，包源码为 fb1e8d1；对应源码/打包及两端原生任务通过，
但该次工作流发行任务被取消，不能声称整个工作流成功。GitHub 有六个发行附件，
AtomGit 当前只有 Mac ZIP/DMG 与明确源码 ZIP，缺 Windows ZIP、SHA256SUMS.txt、release.json。
这里只核对附件清单，未重新验证两站已有文件的字节一致性。下载说明改为使用完整的 GitHub
附件集，未修改镜像、覆盖历史附件或发新版。主分支屏幕连接台是后续开发内容，不在
alpha.5 下载中；会话批次仍等待自动同步和对应原生验证，alpha.6 仅为计划候选。

后续恢复：原始四个包及直接下载的两份发布元数据均匹配 GitHub 官方哈希；重新生成的
元数据因序列化字节不同被拒绝，没有上传。先验证 AtomGit 已有三个附件，再只补三个缺失
附件并逐个回读，现六个文件字节一致。重试 CI 39087609228 后整条工作流通过，
同步任务独立验证六个既有文件、零上传并回读 latest；按标签/最新发行接口均返回原源码
fb1e8d1 的 v0.3.0-alpha.5，名称未加产品名。未覆盖旧附件、未新建标签/发行、未改镜像。
源码 62a1d5b 的新界面已通过原生/图片/包门槛并有内部验收包，仍不包含在已发布 alpha.5 中。
此次恢复不证明后续从 GitHub runner 上传新大包的性能已解决：第一次流式上传仍触发任务超时，
重试验证的是已补齐附件的读取/重复同步与最新标记。
