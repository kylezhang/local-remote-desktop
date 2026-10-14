# Focus release and shared control queues — 2026-10-14

Both production writers carry input and clipboard/file controls. Previously,
release_all cleared the entire pending queue. A focus change could silently drop
unsent consent, text and file offers, leaving session state waiting for a message
which would never arrive.

The mixed-queue regression failed against the original C# and Swift sources.
Both now discard only pending input and earlier releases, prioritize one release,
and retain non-input payloads in order. Repeated release stays bounded. A queue
full of non-input controls reports overload; existing session behavior closes
the connection instead of silently dropping consent or growing the queue.

## Checks performed

- Windows production InputQueue regression: pass after fix, fail before fix.
- ProtocolChecks: pass; authenticated loopback, generated H.264 decode,
  synthetic DPAPI preferences, input coalescing, coordinates and modifier maps.
- ClientChecks default unshown suite: pass; encrypted recording text/files,
  reconnect, 12 sharing cleanup sessions, focus/fullscreen release, quality,
  bilingual shell/layout and pairing/edit states. No live-device opt-in flags.
- MacControllerChecks: Swift 5.9.2 on Linux compiled/executed production core/model
  with explicit platform/session/clipboard doubles; pass. Mixed controls, ordered
  payloads, repeated release, full-control overload, pointer rules and modifier/
  keypad mappings passed.
- Static release/documentation checks and diff whitespace: pass.

No personal clipboard, real OS input, desktop capture, installed Mac app or
permissions were used. Local Windows SDK compiled the changed client; this record
does not claim a new native Apple SDK build or physical input acceptance. CI is
the next native check; existing alpha.3 tags/downloads remain unchanged.

中文：两端原实现均复现丢焦点释放误删剪贴板/文件消息。修复后保留控制消息顺序、
优先释放旧输入，重复释放不扩大队列；全控制消息拥堵时明确断开。
Windows 协议和不显示窗口的客户端回归、Linux Swift 核心/模型检查通过。
未访问个人剪贴板、采集真实桌面或注入系统输入；新版 Apple SDK 与实机验收不在本记录范围。
