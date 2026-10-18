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
native CI and Mac session render review were subsequently completed as below.

## Native CI and internal candidate

Verified source `62a1d5ba4dcbd53399de9a1f900dbf805c88849d` in
[run 39087609228](https://github.com/kylezhang/local-remote-desktop/actions/runs/39087609228).
Source and packaging checks, Windows x64 and macOS Apple Silicon jobs all passed.
No application files changed between session source e198e0a and this commit.

The native Mac job passed 24 English/Chinese layout cases, including session
reconnecting, view-only at 520 × 450 content, and Settings during a session.
Only the two intentional long-pairing-error cases scroll vertically; routine
inspected containers have no overflow. The six new session PNGs were viewed:
toolbar actions fit, long names truncate, state text remains clear and Return to
session remains visible in preferences. These are inactive unshown renders, not
active focus/permission or physical-input acceptance.

Downloaded CI artifacts:

- Windows x64: ID 12121815498.
- macOS arm64 preview: ID 12121582029.
- Mac reports: ID 12121462217.

Both CI checksum manifests and full source identity passed local read-back
verification. The Mac archive passed strict preview bundle/resource checks,
stable bundle identifier and build 33, embedded source marker and full preview
manifest hashes. The Windows archive includes .NET 8 runtime metadata and the
expected session interface marker. Each archive's GPL source snapshot validates
against the same commit; their uncompressed source file maps match.

Three unchanged CI archives were copied under explicit internal candidate names
in the task's work directory, with candidate.json, SHA256SUMS.txt and bilingual
README-FIRST.md. The working client was not replaced. The app version is still
0.3.0-alpha.5, but these newer development packages are distinct from the published
alpha.5 at fb1e8d1. Alpha.6 remains planned, not published.

| Internal candidate file | SHA-256 |
|---|---|
| Windows x64 ZIP | 09e62c609c148862c57d963a33698ffa2b786e2abe06bf13e65a5933926dc014 |
| Mac arm64 ZIP | 254ce17fd185fe1ae2762ff6980edf786b708dcf4989e29a00f6ee76a43e6f22 |
| Mac arm64 DMG | cbc5d2507243548d062ad95e5c79c385798be1978f65af97d0f2db0c52066c02 |

The later Publish requested preview job exhausted its 45-minute limit while
uploading an existing alpha.5 Windows attachment. That historical cancellation
does not invalidate the completed native jobs or these candidate hashes. The
old release's missing attachments are handled separately from the development
candidate; no newer binaries overwrite alpha.5.

After the original alpha.5 attachments were restored and read back byte-for-byte,
the publication job was retried. Run 39087609228 attempt 2 is now successful;
the sync step independently verified all six old-release files and read back
latest, with zero uploads. This closes the old-release parity/status item.
It does not change this internal candidate, replace its source or certify future
large-upload throughput. See [the recovery record](release-status-2026-10-18.md).

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
合成会话和仅查看图片已复核。后续源码 62a1d5b 的源码/打包及两端原生 CI 通过，
24 个 Mac 双语场景通过，六个新增会话图片已查看。下载后复核两端包的哈希、来源、
Mac 构建号 33/资源与 Windows .NET 8 运行时，独立内部验收包已就绪。
未更新在用应用，真实双机和系统授权待集中验收。

补充：同一应用源码 e198e0a 的完整 Swift 文件夹具链通过，包含传输调度、8 MiB 节流、
Unicode/空文件/分块、哈希、撤销与临时文件清理；Apple 服务和哈希边界使用 Linux 测试替身，
不代表 Mac 原生剪贴板实测。Python 3.12 下的五项包来源/完整性拒绝检查通过。
已将导航与焦点 U2、真实状态 U3、会话同步范围 C2 写入双语实测清单，均仍为未执行。
