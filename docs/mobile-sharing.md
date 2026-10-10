# Mobile sharing feasibility

[简体中文](zh-CN/mobile-sharing.md) · [Product direction](product.md)

Checked against official documentation on 2026-10-10. This is a capability study,
not implemented support or a release commitment. The existing controller-first
mobile version milestones remain unchanged. Mobile sharing is not permanently
excluded from the product.

**Share the screen** and **accept remote input** are independent capabilities.
Pairing alone cannot authorize an OS screen-capture session. An ordinary user
should not be asked to re-pair for transfers, but required system screen-sharing
consent must remain explicit.

| Platform | Screen sharing path | Receiving remote input |
|---|---|---|
| Android | MediaProjection provides capture with user consent; each projection session needs consent | AccessibilityService can dispatch taps/swipes. Feasibility path exists; authorization, text entry, protected content and device/version compatibility require native validation |
| iOS/iPadOS | Official screen-capture/broadcast frameworks provide a path; choose APIs against supported SDK/system versions | Apple provides remote control in specific system features such as FaceTime. This does not establish a general third-party injection API usable by this product; complete cross-app control is not promised |
| HarmonyOS | AVScreenCapture exposes screen capture and video-buffer/surface APIs | Public application input capabilities, permissions and distribution feasibility require separate verification; capture APIs alone do not prove remote control |

The design keeps a future mobile **Share this device** entry, initially illustrated
as **screen sharing only**. Show a remote-input option only when an implementation
and its permissions have been validated. Production builds must not present a
mockup as an available feature. Keep local Stop reachable and negotiate capability
with the viewer so pointer/keyboard controls do not imply unavailable input.

Do not assign a new version or promise unattended, locked-screen, protected-content
or universal app control from this study. First finish Windows ↔ macOS; the mobile
controller milestones keep their existing scope, followed by a native sharing
feasibility prototype before scheduling receiving roles.

## Official references

- [Android MediaProjection](https://developer.android.com/media/grow/media-projection)
- [Android AccessibilityService](https://developer.android.com/reference/android/accessibilityservice/AccessibilityService)
- [Apple screen capture](https://developer.apple.com/documentation/screencapturekit) and [ReplayKit](https://developer.apple.com/documentation/replaykit)
- [Apple FaceTime remote control](https://support.apple.com/guide/iphone/request-give-remote-control-a-facetime-call-iph5d70f34a3/27/ios/27)
- [HarmonyOS native AVScreenCapture](https://developer.huawei.com/consumer/en/doc/harmonyos-references-V13/native__avscreen__capture_8h-V13)

The distinction between system-only features and an unverified third-party input
path is a product inference; it is not a claim that Apple devices have no remote
control capability.
