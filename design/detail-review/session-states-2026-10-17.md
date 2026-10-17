# Permissions, failures and session controls

[简体中文](#中文) · [Session preview](desk.html?lang=en&state=session) · [Browser captures](states-overview-v3.png)

2026-10-17. Extends the accepted screen-desk direction with focused interaction
states. **Design preview only:** no native app, OS permission, credential, capture,
input, file transfer, network protocol or release was changed.

## Decisions

- Owner clarification: remove the fictional Project notes/Workspace application.
  The canvas now uses a neutral display symbol and "Remote desktop preview" with
  an explicit actual-desktop explanation. It must not suggest a built-in notes app.

- Capture and input remain independent. A confirmed capture-denial fixture keeps
  Start unavailable and provides a focused guide. Missing input permits an
  explicitly labeled view-only role. Guidance/checking never silently grants
  permission or starts accepting. Native adapters must distinguish confirmed
  denial, unknown status and pending ScreenCaptureKit screen selection: an old
  preflight Boolean alone must not disable the real system picker/start flow.
- Guides expose one system-settings action and application-location recovery.
  They do not suggest deleting permission records, resetting TCC or reinstalling.
  System-version-specific labels/picker behavior must come from native adapters;
  the web guide is an illustrative Mac permissions path, not an OS automation.
- Listener failure is specific, with Retry, collapsible details and Copy diagnostics.
  Technical codes remain inside troubleshooting. Retry preserves the active error
  fixture; it does not falsely claim recovery. Expanded details scroll into view
  inside the content area. The error fixture is an occupied-port example, not a
  generic explanation for all native failures.
- Target-unreachable feedback retains the connection form and quality choice.
  Password mismatches remain a separate inline error. Manual IPs have no invented
  device identity. Preview passwords are fictional, and secure field values were
  not extracted as browser acceptance evidence.
- One compact session bar holds device/status, options, full screen and visible
  Disconnect. It leaves the screen dominant. Back navigates without disconnecting;
  a return-to-session action remains reachable from Home. Rendering a screen must
  never create a session; an isolated check exposed and corrected that boundary.
- Session quality/text/file options affect only the current fixture. Global
  clipboard settings remain a ceiling: locally disabled features cannot be enabled
  in the session sheet. Disabling file sync cancels the fictional transfer notice.
- Reconnecting keeps a fictional desktop underneath a quiet interruption layer,
  with Cancel and Retry. Retry simulates recovery; Cancel clears the fixture and
  returns to the device list. No live reconnect engine was added.
- Transfer progress/completion are temporary notices, without another authentication
  step. The 64% value is fixed sample content. Full screen expands only the preview
  frame, never invokes browser/OS full screen; Escape restores the preview frame.

## Review evidence

JavaScript syntax, whitespace, bilingual keys and the prior isolated role/form
fixtures passed. Actual local-browser review covered 32 language/size/state
combinations: English/Chinese, 600 × 680 / 420 × 568, capture denial, input denial,
listener failure, active session, view-only session, reconnecting, transfer and
completion. The permission fixtures select Mac chrome; this is not a claim that
every state was separately tested in both real operating systems.

No root horizontal/vertical overflow was measured. The accepting footer and session
Disconnect remained inside the preview frame. Long permission/error content may
scroll inside its own region; it is not clipped to hide content.

Browser interaction checks observed: file-sync revocation removes the transfer;
permission checking leaves a denied fixture denied; retry retains real error
details; unreachable-target retry preserves the selected quality and visible form;
globally disabled file sync stays disabled in session options; reconnect Cancel
returns to the list; Escape exits preview expansion. Secure password-field DOM
inspection was not used as proof of retention; draft preservation is covered by
isolated fixtures. Screenshots were saved from actual browser pixels and reviewed.

`state-*-v3.png` retain surrounding review context. The app crops and contact sheet
are browser captures of fictional content, not native screenshots. Pending work:
native UI mapping, installed font/layout/accessibility verification, actual system
capture/control access and real two-device acceptance. These designs do not resolve
the previously reported input/startup failures.

## 中文

本轮延续已认可的视觉方向，补齐权限、失败、会话和传输状态；**仅更新离线设计预览**。
没有改客户端、系统权限、密码、协议或发行版。

根据用户反馈，去除“项目笔记”和虚构文档窗口，改为中性屏幕符号与“远程桌面预览”，
明确连接后显示对方真实桌面，避免被误认为内置笔记功能。

屏幕与控制权限分开；确认录制被拒绝时给出指引，缺少控制权限时明确标记“仅查看”。
原生实现必须区分拒绝、未知、等待系统屏幕选择，不能只凭旧 preflight 布尔值阻断系统选择器。
指引提供系统设置与定位应用，不建议删除授权、重置 TCC 或重装；网页按钮不修改真实系统。

监听失败有重试、展开详情和复制诊断，技术码收在故障排查内；重试不假装成功。
目标不可达与密码错误分开，保留表单草稿。工具栏保持紧凑，断开始终可见；返回首页不结束会话。
全屏只模拟展开预览，Escape 恢复，不进入真实系统全屏。
会话选项不覆盖默认设置，不能重新打开全局关闭的剪贴板权限。重连可取消/重试，文件进度临时展示，
不再次配对；64% 为固定示例，不是真实传输。

语法、双语键和原有隔离状态检查通过。实际网页检查覆盖中英文、两种窗口尺寸与八类状态的 32 个组合，
未发现根区域横向/纵向溢出，开始/停止和断开保持在预览框内；长内容内部滚动。
权限示例会切换 Mac 外观，不能据此声称全部状态都已在两个真实系统上验收。
交互抽查通过：关闭同步移除传输、检查权限不虚假放行、重试保留错误详情、目标不可达保留画质与表单、
会话不能突破全局关闭选项、取消重连返回列表、Escape 退出预览展开。
没有提取安全密码框的值作为网页验收证据，密码草稿保留由隔离检查覆盖。

总览为真实网页像素截图，均为虚构内容，不是原生实机截图。
下一步整理原生实现对应关系，再按确认的设计落地，并独立验证字体、权限和真实双机操作。
