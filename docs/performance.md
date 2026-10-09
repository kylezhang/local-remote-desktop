# Resource use and performance

[简体中文](zh-CN/performance.md)

Sharing a desktop continuously uses CPU, memory and network bandwidth. LocalRemote
does not yet have an end-to-end hardware benchmark or an hours-long soak result.
The current implementation is not a guarantee against every hang or memory leak.

## Controls on resource growth

- Network receives, pipe reads, input queues and reconnect delays wait for work;
  the inspected loops do not continuously poll without a wait. Reconnect waits
  three seconds; authentication and network receive deadlines are bounded.
- Mac capture uses a three-frame ScreenCaptureKit queue and permits at most two
  encoder submissions in flight. Overload drops raw frames before encoding.
  VideoToolbox hardware encoding is required. This is a source review, not a Mac
  resource measurement. System capture/encoder start and stop calls can still hang.
- Windows keeps one latest frame for display, with a 256-item input queue that
  coalesces pending pointer moves. FFmpeg diagnostic output is drained through a
  fixed buffer instead of accumulating until process exit.
- Disconnected Windows clients stop the display timer. Closing the window directly
  terminates the decoder it owns, including when application shutdown prevents
  asynchronous cleanup from finishing. No unrelated FFmpeg processes are stopped.

Windows currently uses FFmpeg software decoding and WPF bitmap copies. Each decoded
BGRA frame allocates a new array: 1920×1080×4 bytes at 60 fps is approximately
498 MB/s of allocation. That is allocation traffic, not retained memory or LAN
bitrate. Frequent garbage collection can affect responsiveness even with a flat
memory graph. Reusable frame buffers and hardware decode/rendering need further work.

## Measured scope: 2026-10-09

Each case fed paced synthetic H.264 to the production Windows decoder for 40 seconds.
The driver includes the producer; capture, LAN transport and WPF display were absent.
This Windows machine reported 32 logical processors. CPU below is normalized to
the whole machine, so values are not comparable to a single-core percentage or
an estimate for another PC.

| Case | Decoded fps | Driver / FFmpeg private memory range | Driver / FFmpeg average CPU |
|---|---:|---|---|
| 1280×720, 30 fps | 29.73 | 25.6–36.8 / 57.3–57.7 MiB | 0.77% / 0.69% |
| 1920×1080, 60 fps | 59.46 | 120.6–147.1 / 76.2–84.1 MiB | 1.70% / 1.62% |

The sampled memory did not continually rise during these short runs. Both cases
had about ten generation-2 collections per second, confirming allocation pressure;
this is not evidence of a leak-free long session. The second case shares the driver
process with the first. Do not interpret its driver memory as isolated app usage.
Both decoders exited after disposal. Separate real WPF/mock-peer tests verified
disconnect, quality changes and closing a connected window.

The deployed real GUI was also observed disconnected for 30 seconds: average
whole-machine CPU 0.010%, private memory 106.3→106.1 MiB, responsive, one window
and no FFmpeg child at the final snapshot. This is an idle observation on the same
32-logical-processor PC, not streaming performance. [Idle samples](../tests/results/resources-idle-2026-10-09.json).

The resource tests also drain a logical 256 MiB diagnostic stream with bounded
allocation, check cancellation and reject oversized frame dimensions before starting
FFmpeg. Raw samples: [resource results](../tests/results/resources-2026-10-09.json).

Run with .NET 8 SDK and FFmpeg on Windows (20–600 seconds per case):

```powershell
dotnet run --project tests/ResourceChecks/ResourceChecks.csproj -c Release -- 40 resources.json
```

Use **Smooth** to reduce the target to 720p/30 when resources or responsiveness are
constrained. Standard and High target up to 1080p/60 with different bitrates; actual
dimensions preserve the Mac display aspect ratio. Full-session CPU, Mac energy use,
sleep/wake, repeated reconnects and long-session behavior still require measurement.
