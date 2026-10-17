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

Native macOS layout/SDK/package verification and final-source Windows CI are
pending when this record is created. Review the Mac artifact images after those
jobs pass. Do not equate the local Swift parse with an Apple SDK build.

No real capture, personal clipboard, OS input, SSH, Keychain/TCC change,
installation, deployment, new version/tag or release is performed by this batch.
Earlier input/listener failures remain separate P0 acceptance items. Session
navigation/toolbar changes are the next batch, not part of this one.

中文：本批更新两端设备展陈、按需配对、接受连接与连续设置分组。Windows 完整后台回归、
字体及页面图片检查通过；设备卡片拉伸已在图片复核中发现并修正。默认值、会话选项及全局
剪贴板允许范围保持原有边界。Mac 仅查看按钮如实提示当前输入能力，不代表已修复系统授权。
Swift 语法、模拟签名/缓存/打包及新旧资源校验通过，原生结果等待对应源码的 CI。
没有升级正在使用的应用、改权限或发版。
