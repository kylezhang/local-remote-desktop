# Screen-desk task pages

2026-10-17. Second native UI batch; interface revision
`screen-desk-2026-10-17-tasks`, Mac internal build 32. Public version is unchanged.
No installed client is replaced by this source update.

## Scope

- A two-column device gallery uses original generic display art. Names/platforms
  remain actual discovery/authenticated metadata. No fake favorite, remembered
  credential or desktop thumbnail is created. Offline records remain selectable
  for manual connection. Credential reads still follow the existing deliberate
  connection path; decoration adds no secret reads.
- One Add target action opens the existing pairing form. Windows presents an
  owned bottom panel; Mac retains a native sheet with a fixed action footer and
  internally scrolling fields. Cancel, errors, remember/forget and role-conflict
  behavior remain bound to the existing controller.
- Accept uses centered local identity, vertically grouped IP/password and fixed
  bottom Start/Stop. Existing name/password editors, failed-save protection,
  starting/stopping ownership and explicit permission actions remain.
- Mac's Start label discloses view-only access when event posting is unavailable.
  It does not infer denial from a legacy screen Boolean, silently grant permissions,
  read Keychain for decoration or reset TCC. Actual permission recovery is not
  established by this UI label.
- Settings becomes four continuous General / Connection / Clipboard / About groups.
  Next-connection defaults remain separate from current-session choices; global
  clipboard consent remains a ceiling. Source identity/diagnostics move into
  expandable troubleshooting instead of occupying the main preferences list.
- Build 32 requires the new generic device image. Builds 30/31 retain their
  original valid bundle inventories; published packages are not modified.

## Local verification

Windows production/ClientChecks build: passed with zero warnings.
The complete unshown ClientChecks passed, including bilingual layout, glyph runs,
pairing failure/cancel/drafts, consent/default boundaries, active listener/editor
guards, small-window fixed actions, generated video and recording-service
clipboard/file/session cleanup. No visible window or real OS input was used.

Visual review found the initial UniformGrid stretched device cards vertically.
Top alignment and a bounded-card regression check corrected it; the complete
Windows suite passed again. Settings default-size fit and small-window horizontal
fit are measured; genuinely long content may scroll.

English/Chinese production WPF content renders were inspected for connection,
Accept and grouped Settings. They are inactive unshown test renders without
native chrome, not installed-app screenshots.

Swift frontend syntax passed in local Linux. Thirteen mocked signing cases,
four cache methods and the mocked package pipeline passed. Additional bundle
checks reject missing/wrong-resolution device art and retain build-30/build-31
compatibility. Release hygiene/link/localization checks passed.

## Cloud and acceptance

Verified on 2026-10-18 against source
`20f4ecda0ac6e4d224581f13df07dfe8b5d5945d` in
[GitHub Actions run 39017589964](https://github.com/kylezhang/local-remote-desktop/actions/runs/39017589964):

- Source and packaging checks: success.
- Windows x64: success, including the production build, ClientChecks and package.
- macOS Apple Silicon: success, including native Swift fixtures, production
  Apple SDK build, unshown layout checks and explicit ad-hoc preview packaging.
  This verifies neither notarization nor installed permission approval.
- All 18 English/Chinese Mac layout cases passed. Routine pages have no measured
  vertical overflow. The two intentionally long-error cases scroll inside the
  pairing form; Cancel/Connect remains outside that scroll area.

Downloaded report artifact `macos-reports-20f4ecda0ac6e4d224581f13df07dfe8b5d5945d`
(ID 12095875773). Visually inspected the English/Chinese gallery, Accept, grouped
settings, known-peer target and long-error target PNGs. Long names wrap; unavailable
peers remain distinguishable; password and footer actions remain visible. These
are inactive, unshown native content renders without window chrome; they do not
prove active-window tint/focus or the installed sheet/permission experience.
Reports and images stay outside Git because they are generated evidence.

The first Mac cloud attempt exposed a test-only private-setter compile failure.
The fix seeds the notice through a same-file extension in a temporary harness copy;
the shipped controller retains its private setter. The final source also keeps
expanded permission recovery details inside the Permissions disclosure, while
retaining the visible control-permission action and view-only Start label.

CI package artifacts: `windows-x64-20f4ecda0ac6e4d224581f13df07dfe8b5d5945d`
(ID 12095253740) and
`macos-arm64-preview-20f4ecda0ac6e4d224581f13df07dfe8b5d5945d`
(ID 12095447870). These are development artifacts, not a new published version.

The workflow as a whole is **not green**: its later Publish requested preview job
verified existing GitHub alpha.5 assets and three existing AtomGit attachments,
then timed out uploading the missing Windows ZIP after three attempts. Native
jobs succeeded independently. Do not describe AtomGit/GitHub release attachment
parity as confirmed or overwrite immutable alpha.5 assets with this batch.
Existing Capture Sendable and legacy Keychain API warnings remain; a warning-free
Mac build is not claimed.

No real capture, personal clipboard, OS input, SSH, Keychain/TCC change,
installation, deployment, new version/tag or release is performed by this batch.
Earlier input/listener failures remain separate P0 acceptance items. Session
navigation/toolbar changes are the next batch, not part of this one.

中文：本批更新两端设备展陈、按需配对、接受连接与连续设置分组。Windows 完整后台回归、
字体及页面图片检查通过；设备卡片拉伸已在图片复核中发现并修正。默认值、会话选项及全局
剪贴板允许范围保持原有边界。Mac 仅查看按钮如实提示当前输入能力，不代表已修复系统授权。
Swift 语法、模拟签名/缓存/打包及新旧资源校验通过。2026-10-18 已确认源码 20f4ecd 的
源码/打包与两端原生 CI 通过，并查看 Mac 中英文展陈、接受连接、设置及配对图片。
18 个布局场景通过；长错误仅表单内部滚动，底部操作保留。图片来自未显示窗口的原生渲染，
不代表系统授权或真实双机验收。后续发行同步任务上传既有 alpha.5 Windows 附件超时，
不能据此声称整个工作流成功或两站发行附件齐全。没有升级正在使用的应用、改权限或发新版。
