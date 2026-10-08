# 2026-10-08 构建与测试记录

环境：Windows x64，.NET SDK 8.0.425。SDK 仅放工作目录，未改用其他仓库托管服务。私有 AtomGit 仓库：`zkxw2008/remote-mac-lan`。

| 检查 | 结果 | 意义与限制 |
|---|---|---|
| Windows Release build | 通过，0 warnings / 0 errors | C# / XAML 完整工程编译，不代表与 Mac 实机联通 |
| WPF 应用启动 | PASS，运行 3 秒后正常关闭 | 仅进程启动烟测，未检查真实 Mac 画面 |
| 正常 HMAC 双向认证与 AES-GCM 报文 | PASS | 合成本地 Host 校验客户端 proof，客户端校验 Host proof |
| TCP 单字节分段发送 | PASS | 不依赖单次 read 恰好返回一个报文 |
| 篡改 GCM tag | PASS（拒绝） | 数据完整性检查有效 |
| 重放计数器 0 报文 | PASS（拒绝） | 同连接重放被方向计数器拒绝 |
| 错配对密钥 | PASS（拒绝） | 不建立已认证会话 |
| 超长长度头 | PASS（拒绝） | 分配大缓冲区前校验长度 |
| FFmpeg 实际 H.264→BGRA | PASS，320×180 至少 69 帧 | 合成 Baseline/zerolatency H.264 数据；没有证明 1080p60 或 Mac 端输出兼容 |
| 当前用户 DPAPI | PASS | 随机测试配对 roundtrip，通过后删除测试密钥文件 |
| macOS | 源码静态检查，未编译 | 无 Apple SDK / Mac 设备；类型检查、权限、采集、编码、输入、自启动均待实机验证 |

测试程序在 `tests/ProtocolChecks`，不含真实用户密钥，临时合成视频测试后删除。Shell 下载与本地回环测试最初受运行沙箱网络限制，在获准的执行环境完成；没有变更产品安全模型。

没有测试：真实桌面端到端、性能、休眠/唤醒、真实断线重连、FileVault/登录前行为、Mac 自动启动、中文 IME、系统保留快捷键。没有实现：Media Foundation、D3D11、mDNS、剪贴板。

后续用户反馈：更新至 8199e16 后 Mac Release 编译、链接和应用签名已成功；普通锁屏密码框可接收字母/字符，但数字键存在故障。已补齐数字小键盘事件标记，尚待用户确认故障键所在区域及复测。此反馈不等于所有桌面功能或数字键已验收通过。
