# Architecture

[简体中文](zh-CN/architecture.md)

The native Mac application runs in the logged-in user session. ScreenCaptureKit captures the primary display's real desktop. VideoToolbox requests hardware H.264, Baseline, real-time, no B frames and periodic keyframes. AVCC NAL units become Annex B; keyframes include SPS/PPS. No audio capture or recording file.

One full-duplex IPv4 TCP connection multiplexes encrypted video and JSON control. TCP_NODELAY, bounded work, timeouts and reconnect favor a small implementation. Slow sends can compete with heartbeat/metrics and increase latency. Capture bounds outstanding raw frames and skips raw frames under pressure rather than dropping encoded reference frames. Each connection constructs a fresh encoder/decoder stream. This is not a measured end-to-end latency guarantee.

Windows verifies the peer before launching FFmpeg. The decoder takes Annex B stdin and outputs BGRA to a replaceable IVideoDecoder adapter. One software decoder thread and low_delay reduce buffering; a latest-frame slot and WPF WriteableBitmap avoid a growing UI queue. Receiving/decryption runs off the UI thread. Pixel allocation/copy and software decode still cost CPU; Media Foundation/Direct3D are not implemented.

The bounded input queue coalesces consecutive pending mouse moves or consecutive
clipboard changes, without crossing button/key transitions. Overload disconnects
rather than silently losing key-up. Focus loss and disconnect release held input.
Normalized pointer coordinates exclude letterbox bars; active drags clamp outside
the picture. Mac injects CGEvent only with Accessibility permission. Windows
system-reserved shortcuts are not universally intercepted.

Optional clipboard text uses the same authenticated JSON channel after capability
and mutual-consent negotiation. Platform adapters serialize clipboard access on
the UI/main thread; Mac writes stay off main. Portable state suppresses received
changes, limits data to 32 KiB and bounds busy-write retries. Timers poll at 500 ms;
Mac timer events coalesce during a slow send. Windows inserts text ahead of paste
input in the shared queue. See [user behavior](usage.md#text-clipboard-sync-development)
and [protocol](protocol.md#optional-text-clipboard).

Metrics are decoded FPS and incoming H.264 Mbps on Windows, plus mean Mac encoding callback time. Encoding time includes encoder queuing and excludes network time. There is no trustworthy per-frame decoder or total input-to-display latency metric in this subprocess pipeline.

PSK stored in Keychain/DPAPI authenticates each connection; independent direction keys protect video and input. Protocol limits and diagnostics are documented separately. Application files and source names use LocalRemote; stable bundle/Keychain identities and the wire protocol remain compatible. See [naming migration](naming.md).
