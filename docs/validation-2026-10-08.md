# 原生客户端修复候选验证记录

本轮集中原生 Swift Host + Windows WPF 客户端，暂停 WebRTC 迁移。此记录区分自动检查通过与真实 Mac 尚未验证的内容。

## 修改

- 视频解码改为单线程、low_delay，避免 FFmpeg 自动帧线程引入额外缓存。
- 连续尚未发送的鼠标移动合并为最新位置；不会跨越按键或鼠标按钮消息合并。队列拥堵时断开，`release_all` 可清空积压。
- 拖动超出画面边界时继续发送边缘坐标；释放鼠标捕获、键盘焦点切换时释放远程输入。
- 远程画面区域禁用 Windows 本地 IME，物理按键由 Mac 输入法解释。Windows 和 Mac 的输入法状态不是自动同步的；中文需要 Mac 选用中文输入法。
- Host 心跳报告收到的输入消息数、提交的 CGEvent 数、权限拒绝数和无效消息数；Windows 展示汇总并可复制不含密钥、IP 和实际输入内容的诊断。
- Mac 新增“本机键盘自检”，在 Host 自己的临时窗口中验证字母、主键盘数字、小键盘数字。需主动点击，绝不自动往用户工作文档输入。英文输入法下预期 `a01234567890123456789`。

自检复用同一输入映射与事件创建，但以 `CGEvent.postToPid` 仅投递到 Host 自己的进程，避免焦点改变后落到其他应用。真实远程输入仍走系统 HID tap。自检通过不等于其他应用、锁屏或密码框一定接受事件。参见 [Apple postToPid](https://developer.apple.com/documentation/coregraphics/cgevent/posttopid(_:))。
- Mac 只允许两个采集帧同时处于编码/发送流程中；阻塞时跳过未编码的原始帧，避免无限积压。不随意丢弃 H.264 参考帧。此项尚需 Mac 原生验证。

## Windows 自动验证

`tests/ClientChecks` 创建真实 WPF MainWindow，使用临时随机密钥、本机模拟 Host、实际加密协议和真实 FFmpeg 解码。测试窗口离屏且不主动激活，不读写用户保存的连接，不向真实 Mac 发输入。

覆盖实际客户端认证与图像显示、WPF 路由的字母/数字/小键盘/Enter/Ctrl/Alt/Win 按下释放顺序、客户端指针转换与越界拖动、右键、路由滚轮、焦点丢失释放输入、权限拒绝/恢复显示，以及断开后的资源与界面恢复。

这里的键盘和滚轮是程序生成的 WPF 路由事件；鼠标调用客户端同一处理入口。**不等于测试了实体键盘、Windows 系统快捷键截获，或 macOS CGEvent 的最终接收。**Ctrl+Alt+Delete 属于 Windows 安全注意序列，不会透传；部分 Win/Alt 系统快捷键仍可能由 Windows 处理。

`tests/ProtocolChecks` 覆盖认证正确/错误密钥、密文篡改、重放、TCP 分片、异常长度、实际 H.264 → BGRA、Windows DPAPI、10,000 次鼠标移动合并、点击拖动消息边界、队列溢出与释放，以及黑边/非法坐标处理。

## 量化解码比较

`tests/LatencyChecks` 将同一 1280×720、30fps、Baseline H.264 合成测试图按实时速度依次送给旧、新解码配置，测量每帧写入解码管道到完整 BGRA 输出的耗时，略去开始十帧。

| 配置 | 样本数 | 中位耗时 | P95 |
|---|---:|---:|---:|
| 原 FFmpeg 自动帧线程 | 123 | 569.49ms | 583.84ms |
| 单线程 low_delay | 138 | 70.84ms | 87.41ms |

见 `tests/results/decoder-latency-comparison.json`。这是本机特定硬件与 FFmpeg 上的一次 A/B 测量，包含管道/解码/像素转换缓存，不包含 Mac 采集、编码、LAN 或 WPF 显示，也不是跨机的完整延迟。此结果支持修改解码配置，不能据此宣称已解决全部卡顿。

## 尚未通过的验收

开发环境为 Windows，没有 macOS SDK 和 Mac shell/桌面访问。Swift 文件通过 tree-sitter 静态语法解析，**并未完成原生编译、类型检查或执行**。真实 Mac 的辅助功能权限、CGEvent 被目标应用接收、中文输入、锁屏、采集背压处理和跨机延迟仍未验证。不能宣称输入失效已彻底修复。

“已提交”是调用 CGEvent.post，并非 macOS 返回操作成功；该 API 没有目标应用接收确认。旧 Host 不报告计数时，Windows 明确显示“Mac 当前版本未回报输入诊断”，不把零当作未收到。

开发签名情况保持原有边界：固定有效的签名身份可减少权限身份变化；ad-hoc 构建更新后仍可能需要重新授权。未提供开发者证书的情况下，没有承诺永久免授权。FileVault 预启动或用户登录前控制不受支持。

## 重现检查

Windows 安装 .NET 8 SDK，FFmpeg 可从 PATH 找到，然后在仓库目录依次运行：

```powershell
dotnet run --project tests/ProtocolChecks -c Release
dotnet run --project tests/ClientChecks -c Release
dotnet run --project tests/LatencyChecks -c Release
```

Mac 原生构建与最后验收留到候选版本准备完成后一次进行：先结束 WebRTC 桥接与其他客户端，按原安装脚本更新 Host，在英文输入法下执行“本机键盘自检”，再检查真实 Windows → Mac 输入及短时使用。故障时诊断计数能区分客户端未发送、Host 未收到、权限拒绝、事件无效或已经提交但目标未接受，不要求反复猜测授权问题。
