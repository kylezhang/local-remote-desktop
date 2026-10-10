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

指标是 Windows 解码 FPS、收到的 H.264 Mbps、Mac 编码回调均值。编码时间含排队，不含网络；当前子进程流水线没有可靠逐帧解码或端到端指标。程序文件和源码统一 Local Remote Desktop，系统身份、钥匙串及通信协议保留兼容，见[命名迁移](naming.md)。

开发版本支持手填/自动生成配对密码，用随机盐与 PBKDF2-HMAC-SHA256（600000 次）派生 PSK；旧原始密钥兼容保留。
Mac 在凭据队列派生并缓存，Windows 在界面线程外派生；显式保存更新同一钥匙串项并断开旧连接。凭据仍由 Keychain/DPAPI 存储保护。

## 桌面双角色规划

当前 Mac 实现共享，Windows 实现控制。Windows MainWindow 还直接管理连接、输入、显示和剪贴板，
已有 IVideoDecoder 可复用，但整体还未分离为独立会话模块，需要逐步整理。

反向链路新增 Windows 采集/编码/输入注入及 Mac 解码/显示/输入采集。
Windows 候选是 [DXGI 桌面采集](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/desktop-dup-api)及
[SendInput](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput)；输入受进程权限等级约束，
不承诺 UAC 安全桌面或登录前控制。alpha.2 Mac 控制端源码已实现
[VideoToolbox 解码](https://developer.apple.com/documentation/videotoolbox/vtdecompressionsession-api-collection)
和 AppKit 输入采集：ControllerSession 管理认证连接及有界发送线程，ControllerModel 只呈现
最新画面，取消过期回调及重连。原生解码、呈现和输入仍待验收。
Windows 当前通过 FFmpeg GDI/Media Foundation 硬件编码及 SendInput 实现共享，尚非直接 DXGI 采集。
范围与检查见 [Mac 控制端预览](mac-controller.md)。

认证和数据格式按共享/控制角色复用；现有辅助功能字段、Mac 修饰键及 Windows 键码不能直接视为平台无关，
需明确能力协商和兼容处理。每次调整都验证原有方向；实现顺序见[路线图](roadmap.md#v030-实现顺序)。

## 文件剪贴板扩展

开发构建通过独立 TCP 47981 传文件，绑定已认证桌面会话及独立文件允许状态。两方向均由 Windows 发起，来源元数据经桌面控制通道通知。自动复用 PSK 认证/AES-GCM，并用新文件连接密钥和桌面令牌，不需额外人工配对。

剪贴板只在主/UI 线程访问，单个可取消后台任务以 32 KiB 分块传输，检查 SHA-256/长度后提交私有临时目录。进度限频，文件路径不混入文字同步，收到写入防回传，新本地复制优先。文件节流给远控留带宽，但不保证端到端延迟。完成文件保留在磁盘，见[文件指南](files.md)。

文字与文件共享在新配置中默认开启，仅用于已认证配对会话；主动关闭会跨启动记住。连接不发送已有剪贴板内容。Mac 用 UserDefaults、Windows 用当前用户 DPAPI 存储选择。
