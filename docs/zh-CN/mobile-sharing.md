# 移动端共享可行性

[English](../mobile-sharing.md) · [产品方向](product.md)

2026-10-10 核对官方文档。这是能力研究，不是已实现支持或发版承诺；既定手机控制端
版本目标不变。“先做控制端”不意味着永久排除手机共享。

**共享屏幕**和**接受远程操作**是两项独立能力。配对不能替代系统屏幕采集授权；
传文件无需再次配对，但系统要求的共享确认仍需保留。

| 平台 | 共享画面的路径 | 接受远程操作 |
|---|---|---|
| Android | MediaProjection 提供采集，每次共享会话需要用户确认 | AccessibilityService 可以派发点击、滑动；存在技术路径，权限、文字输入、受保护内容、机型/版本兼容性需实机验证 |
| iOS/iPadOS | 官方屏幕采集/广播框架提供路径，按支持的 SDK 和系统版本选用 | FaceTime 等系统功能有远程控制；不能据此认定第三方应用也有通用输入注入接口，本项目不承诺完整跨应用控制 |
| HarmonyOS | AVScreenCapture 提供采集、视频缓冲和 Surface API | 普通应用输入能力、权限及发行条件需单独核实，能采集不等于能远程控制 |

设计保留手机端未来的**共享此设备**入口，先演示**仅共享画面**。
验证了输入实现及权限后再显示远程操作选项；正式应用不把设计原型当成可用功能。
本机随时能停止，并向查看方传递能力，不能出现无法接受输入却仍显示键鼠控制的情况。

本研究不直接分配新版本，也不承诺无人值守、锁屏、受保护内容或所有应用可控。
先完成 Windows ↔ macOS，手机控制端保持既定里程碑；移动共享先做原生可行性原型，
再安排接收角色的版本。

官方依据：[Android 采集](https://developer.android.com/media/grow/media-projection)、
[Android 辅助功能](https://developer.android.com/reference/android/accessibilityservice/AccessibilityService)、
[Apple 采集](https://developer.apple.com/documentation/screencapturekit)、
[ReplayKit](https://developer.apple.com/documentation/replaykit)、
[FaceTime 远程控制](https://support.apple.com/guide/iphone/request-give-remote-control-a-facetime-call-iph5d70f34a3/27/ios/27)、
[HarmonyOS 采集](https://developer.huawei.com/consumer/en/doc/harmonyos-references-V13/native__avscreen__capture_8h-V13)。
系统专用功能与尚未验证的第三方输入路径之间的区分是本项目的评估结论，
并不是说 Apple 设备不存在远程控制能力。
