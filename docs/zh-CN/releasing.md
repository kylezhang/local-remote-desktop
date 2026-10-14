# 发版流程

AtomGit 的“最后更新发行版”和主页“最新发行版”是两个选择。发布后应通过 MCP/API
回读 `release_status`，不能只看列表顺序。名称仅使用版本号，如 `v0.2.0-beta.1`。
2026-10-11 将已有 beta 设为 `latest` 时，平台也把 `prerelease` 字段改为 false；
版本号和说明仍明确标记测试版，不代表原生验收门槛已通过。已有 tag 和下载附件不变。
参见[官方更新接口](https://docs.atomgit.com/docs/apis/patch-api-v-5-repos-owner-repo-releases-tag/)。

CI 同步步骤 `scripts/sync-atomgit-release.py` 在 GitHub 发布后自动执行同样的纪律：
令牌仅来自 `ATOMGIT_RELEASE_TOKEN` secret；只补传缺失附件；同名附件校验值冲突时停止
而不是覆盖；全部附件与校验过的 GitHub 字节一致后，才 PATCH `release_status` 并回读确认。

[English](../releasing.md)

1. 确定范围与版本，从 v0.1.0 开始，后续按大/小/修订版本语义维护；两端版本、窗口与更新日志一致，Mac 内部 build number 递增。
2. 同步英文/中文文档；检查已跟踪文件、Git 忽略、差异与敏感信息，不提交构建产物、日志、凭据或证书。不随意改变兼容身份。
3. GitHub Actions 是两端主要原生构建和自动测试环境，本地编译可选。记录成功运行和精确提交，区分 CI 模拟、用户反馈与真实双机验收；用户工作时不打开测试窗或注入输入。
4. 从已提交、干净的源码运行 scripts/package-windows.ps1 打包。附项目 GPL-3.0-or-later 的 LICENSE、NOTICE.md、SOURCE.txt 和 source.zip，同时保留 .NET 许可证、第三方说明和启动指南。source.zip 包含精确提交的源码、构建脚本与文档，接收者无需访问私有仓库即可取得相应源码。生成 SHA-256，确认 ZIP 含测试过的程序集、图标、桌面运行时及匹配源码；FFmpeg 外置，不发布未验证 Mac 二进制。
5. 源码推送 AtomGit，由现有镜像触发 GitHub CI，不另行推送 GitHub、不改同步流程。在已测精确提交上创建注释 tag vX.Y.Z，先准备两站草稿发行版，名称只写版本号，中英文说明、源码和校验值一致，附件使用同一套云端构建字节。发布前核对两站 tag 和下载附件；上传部分失败时只重试缺失且匹配的附件。已发布 tag 和产物不改指向，错误另发版本。
6. 继续更新约定的 Windows 启动目录；空闲时部署已验证包，正在使用时等退出再更新，不强制关闭重开或多启动实例。仅保留一个产品启动文件，保留连接记忆；延后任务只部署最新已验证候选，构建产物不进 Git。

明确标记的测试版可在原生 Mac 验收前发布已测试 Windows ZIP 与 Mac 源码。设 prerelease=true，显著说明 Mac SDK/界面/钥匙串和真实两机待验证，不附 Mac 二进制。此规则不允许以正式版发布、不替代 Mac 原生二进制验收，也不代表路线图功能全部完成。

发版说明区分已实现、自动测试、用户反馈与未验证平台。不隐式改变仓库可见性或选择开源许可证。

2026-10-14 维护者明确暂不做互动实机测试，授权走完整编译/发布流程。因此 alpha.3 可在云端
原生编译和包检查通过后附 Mac 二进制，必须显著披露实机验收待完成。
`.github/release-request.json` 记录这次特定预发布；`scripts/ci/publish-preview.sh` 核对当前运行的
精确附件，创建注释 tag 和草稿，下载核验后发布。仅本仓库 main 分支运行，PR 不可发布，
使用 Actions 自带令牌，不需要维护者 PAT。不发布正式版、不改 AtomGit 镜像流程；
AtomGit 通过 Actions 的 API 同步步骤接收同一套字节；MCP 保留为查看与恢复手段。

AtomGit 官方 API 未文档化草稿参数。先建立版本入口，在任何上传前核对全部已有附件，
六个附件均匹配 GitHub API 摘要、归档校验清单完整且不重复、来源符合不可变 tag，
全部回读通过后才设置状态。请求中的 `atomgitStatus`（或 `--atomgit-status`）将主页选择与
稳定性分开；alpha.3 选择 `latest`，不改变 GitHub 预发布标记或版本说明。

Mac 二进制另须通过[签名与分发验收](macos-signing.md)。维护者目前不购买 Apple 开发者会员，明确标注的临时签名、未公证包是支持的分发方式。功能与原生验收决定版本稳定性，不能由是否付费签名决定。须披露首次打开确认，以及临时签名更新后可能需要重新认可辅助功能/钥匙串；不关闭 Gatekeeper、不承诺权限无感保留。共享 Swift 逻辑测试不能代替原生权限验收。

当前临时签名 CI 候选包由 `scripts/package-macos-preview.sh` 生成 ZIP/DMG；首次打开前通过 DMG 的 Applications 入口安装。macOS 14 及以上开始接受连接时在系统选择器中选择主屏幕，辅助功能仍需单独授权，参见[屏幕访问](mac-screen-access.md)。CI 附件是临时下载，长期发行版在两站使用同一套已验证字节。

可选的 Developer ID 分发路径：统一版本并提交后，用 `scripts/package-macos.sh`、Developer ID 指纹及已有钥匙串公证配置生成 Mac 候选包，详见[维护者打包](macos-signing.md#维护者打包)。
流程检查已提交源码，构建、签名、公证并校验最终解压产物，输出不覆盖的候选目录、来源和校验值。
Mac 候选包同样附 LICENSE、NOTICE.md、SOURCE.txt 和该精确提交的 source.zip。
不会安装 Host 或发布；仍需原生首次启动和升级验收后才能打 tag、上传。这里仅完成模拟验证。
