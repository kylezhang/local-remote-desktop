# Architecture

[简体中文](zh-CN/architecture.md)

The native Mac application runs in the logged-in user session. ScreenCaptureKit captures the primary display's real desktop. VideoToolbox requests hardware H.264, Baseline, real-time, no B frames and periodic keyframes. AVCC NAL units become Annex B; keyframes include SPS/PPS. No audio capture or recording file.

One full-duplex IPv4 TCP connection multiplexes encrypted video and JSON control. TCP_NODELAY, bounded work, timeouts and reconnect favor a small implementation. Slow sends can compete with heartbeat/metrics and increase latency. Capture bounds outstanding raw frames and skips raw frames under pressure rather than dropping encoded reference frames. Each connection constructs a fresh encoder/decoder stream. This is not a measured end-to-end latency guarantee.

Windows verifies the peer before launching FFmpeg. The decoder takes Annex B stdin and outputs BGRA to a replaceable IVideoDecoder adapter. One software decoder thread and low_delay reduce buffering; a latest-frame slot and WPF WriteableBitmap avoid a growing UI queue. Receiving/decryption runs off the UI thread. Pixel allocation/copy and software decode still cost CPU; Media Foundation/Direct3D are not implemented.

The bounded input queue coalesces only consecutive pending mouse moves, preserving button/key transitions. Overload disconnects rather than silently losing key-up. Focus loss and disconnect release held input. Normalized pointer coordinates exclude letterbox bars; active drags clamp outside the picture. Mac injects CGEvent into the primary display's logical bounds only with Accessibility permission. Windows system-reserved shortcuts are not universally intercepted.

Metrics are decoded FPS and incoming H.264 Mbps on Windows, plus mean Mac encoding callback time. Encoding time includes encoder queuing and excludes network time. There is no trustworthy per-frame decoder or total input-to-display latency metric in this subprocess pipeline.

PSK stored in Keychain/DPAPI authenticates each connection; independent direction keys protect video and input. Protocol limits and diagnostics are documented separately. Brand changes retain the legacy bundle ID, application path and protocol to avoid unnecessary credential/TCC migration.
