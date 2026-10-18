# Screen-desk session batch

2026-10-18. Interface revision `screen-desk-2026-10-18-session`; Mac internal
build 33. Public version and stable credential/bundle identities are unchanged.

## Behavior and scope

- Page selection does not own the connection. Home and Settings remain usable
  during a session; Return to session reuses it. Competing roles are disabled.
- Leaving the canvas sends release-all and blocks hidden-canvas key/mouse input.
  The normal decoder can continue receiving; returning does not reauthenticate,
  read credentials, replace the decoder or fabricate a connected screen.
- Disconnect while outside the session preserves the current task page.
- A compact native bar retains device/status, Home, options, fullscreen and
  Disconnect. Windows retains a direct quality picker; Mac uses its options menu.
  Real waiting/reconnect and view-only states replace an unexplained black canvas.
- Existing transfer notices/cancel/open/dismiss actions remain temporary. Mac
  now offers temporary text/file sync choices. Effective consent is the
  intersection with global consent, refreshed immediately on changes. Disabling
  files cancels obsolete work; stale acknowledgments/completions cannot restore it.
- Protocol, capture, transport, pairing, credentials and system permissions are
  retained. No account, relay, real desktop placeholder or fake device is added.

## Verification

Windows production and ClientChecks build passed with zero warnings. The complete
unshown regression passed, including five Home/settings/session round trips over
an encrypted loopback peer with generated H.264. It verifies transport/decoder
identity, unchanged credential-read count, release-all and rejected hidden-canvas
input, plus existing quality/focus/fullscreen/clipboard/file/role cleanup.
The 520-wide English/Chinese session and view-only layouts passed and were rendered.

Swift frontend syntax and production controller model tests passed in Linux with
recording services. Navigation preserves the same session, rejects competing roles,
does not route hidden-canvas input and preserves Settings on disconnect. Effective
temporary clipboard consent cannot exceed globals. Apple SDK compilation is a
separate cloud gate; Foundation mocks do not prove native SwiftUI/AppKit behavior.

Expanded temporary-consent checks exposed a real stale busy indicator: revoking
file sync canceled the worker but left the controller's busy/progress state until
an acknowledgment. The consent transition now immediately reports idle status.
The final Swift model regression passed after this correction, including revoked
file cancellation, suppressed stale completion and renewed consent.
The navigation fixture observes each release before the next trip because the
bounded input queue correctly coalesces simultaneous redundant release messages.
Final Windows build and full unshown regression passed; the 520-wide English/
Chinese synthetic-stream and view-only images were visually reviewed. Exact-source
native CI and Mac session render review remain pending.

Follow-up portable verification on the same application source `e198e0a` passed:
the complete Swift file fixture chain covers controller scheduling, paced 8 MiB
send/receive, Unicode/empty/chunked files, SHA-256, revoked/invalid offers and
partial-worker/staging cleanup. AppKit/Darwin/socket and hashing boundaries are
explicit Linux fixture substitutes; this is not native Mac clipboard acceptance.
The five CloudReleaseChecks passed under Python 3.12 with an isolated writable
temporary directory, rejecting corrupted assets, wrong version/source/platform,
unsafe/duplicate inventories and an unrequested stable release.
The physical acceptance checklist now includes session navigation/focus (U2),
truthful waiting/view-only states (U3) and temporary consent boundaries (C2).
Those physical cases remain Not run.

## Boundaries

No installed client was replaced/shown, no SSH used, and no physical desktop
captured. All network checks use loopback fixtures; clipboard and input services
record operations. No personal clipboard, Keychain/TCC change, real input,
release/version/tag or distribution-signing claim is involved. Earlier physical
input/permission/listener P0 acceptance remains separate.

中文：本批将两端导航与会话生命周期分开，增加返回会话，离开画面释放按键并阻止隐藏画面
转发键鼠；保留原连接、解码器和凭据。在设置中断开后不跳页。会话工具栏、等待/重连及仅查看
提示采用原生控件；Mac 增加受全局允许状态约束的临时文字/文件同步选项。
Windows 完整后台回归与 Swift 语法/模拟导航通过。扩展文件选项检查发现取消后的忙碌状态
残留，修正后 Swift 最终回归通过。Windows 最终编译、完整后台回归通过，520 宽中英文
合成会话和仅查看图片已复核；最终源码原生 CI 与 Mac 会话图片待完成。
未更新在用应用，真实双机和系统授权待集中验收。

补充：同一应用源码 e198e0a 的完整 Swift 文件夹具链通过，包含传输调度、8 MiB 节流、
Unicode/空文件/分块、哈希、撤销与临时文件清理；Apple 服务和哈希边界使用 Linux 测试替身，
不代表 Mac 原生剪贴板实测。Python 3.12 下的五项包来源/完整性拒绝检查通过。
已将导航与焦点 U2、真实状态 U3、会话同步范围 C2 写入双语实测清单，均仍为未执行。
