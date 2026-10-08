# 架构

Mac SwiftUI 应用在用户登录会话运行；ScreenCaptureKit 捕获主显示器的当前桌面，不创建新会话。VideoToolbox 强制硬件 H.264，Baseline、无 B 帧、实时编码、每秒关键帧。AVCC NAL 转 Annex B，关键帧前附 SPS/PPS。采集不写文件。

一条 IPv4 TCP 全双工连接复用视频及 JSON 控制。实现最短验证路径，暂不拆第二端口；视频发送会竞争 Host 发出的心跳/指标，所以慢网络可能增加延迟。TCP_NODELAY、3 秒发送超时、15 秒读取/心跳超时；不积累无限视频队列，编码输出直接施加背压，超时断开重新建流。编码不应阻塞太久，仍需真实网络性能测试。新连接重建 encoder，先发送尺寸与 SPS/PPS。

Windows 先验证 Host 身份，再启动 FFmpeg，无 shell 拼接、无录像文件。输入队列有界，拥塞超过 256 条断开释放按键。鼠标移动限流；不会为了丢鼠标移动而丢 key-up。FFmpeg 连续 Annex B 输入，输出 BGRA，最新帧覆盖前帧，每 16ms UI 定时器显示，避免 UI 排队。独立 `IVideoDecoder` 是迁移 Media Foundation/D3D11 的边界。当前逐帧分配/复制以及 FFmpeg 软件解码会消耗 CPU，并不是最终性能实现。

指标：Windows 已解码 FPS、接收 H.264 码率；Mac 编码回调耗时平均值（包含编码器排队，不含网络）。未报告单帧解码耗时或端到端延迟，因为子进程流水线无法可靠对应输入输出帧。不会把接收耗时冒充解码耗时。

CGEvent 在主显示器逻辑坐标注入，使用捕获显示器 bounds 而非编码像素。仅获辅助功能权限时注入。连接结束释放 keys/buttons。Windows 系统键未使用低层 hook，不能保证所有系统快捷键转发。Host 不修改用户键盘布局。

安全存储、认证和加密参见协议；没有未认证控制路径。没有云组件或第三方账号。唯一外部运行依赖为 Windows 的 FFmpeg，本身可以离线使用。
