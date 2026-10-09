# 架构

[English](../architecture.md)

Mac 原生应用在已登录用户会话运行，ScreenCaptureKit 捕获主显示器真实桌面，VideoToolbox 请求硬件 H.264 Baseline、实时、无 B 帧及周期关键帧。AVCC 转 Annex B，关键帧附 SPS/PPS。不采集音频、不写录像文件。

一条 IPv4 TCP 双向连接复用加密视频及 JSON 控制。使用 TCP_NODELAY、有界任务及超时重连；慢发送仍可能竞争心跳并增加延迟。限制未完成原始帧数量，压力下跳过采集帧，不丢编码参考帧。每次连接重建编解码器，不保证特定端到端延迟。

Windows 验证身份后启动 FFmpeg，通过 stdin 输入 Annex B、stdout 输出 BGRA。独立 IVideoDecoder 可替换；单解码线程和 low_delay 减少缓存，最新帧槽及 WPF WriteableBitmap 避免 UI 堆积。接收解密在后台，逐帧分配、复制和软件解码仍有 CPU 成本，尚无 Media Foundation/D3D。

输入队列合并连续待发鼠标移动或连续文本剪贴板更新，不跨过按键/按钮边界；超载断开，避免静默丢 key-up。
失焦/断开释放；坐标排除黑边，拖动越界夹紧。Mac 获辅助功能权限后用 CGEvent 注入，Windows 保留键不保证转发。

可选文本同步在能力声明、双方主动允许后使用同一加密 JSON 通道。平台剪贴板在 UI/主线程访问，Mac 网络写入在后台。
便携状态逻辑抑制收到内容回传，32 KiB 上限，忙时最多保留一条文本重试两秒。500 毫秒检查，Mac 慢发送时定时事件合并，
Windows 将新文本放在粘贴按键之前。行为见[使用指南](usage.md#文本剪贴板同步开发更新)，字段见[协议](protocol.md)。

指标是 Windows 解码 FPS、收到的 H.264 Mbps、Mac 编码回调均值。编码时间含排队，不含网络；当前子进程流水线没有可靠逐帧解码或端到端指标。程序文件和源码统一 LocalRemote，系统身份、钥匙串及通信协议保留兼容，见[命名迁移](naming.md)。
