# 后续与边界

已实现 Phase 0、Phase 1/2 的完整代码路径及 Phase 3 部分；Windows 构建与合成链路测试不代表真实 Mac 联调完成。

优先级：
1. 在 Apple Silicon macOS 13+ 编译、修正 SDK 差异，验证 TCC、Keychain、ScreenCaptureKit、VideoToolbox、CGEvent；按验证清单完成两机验收。
2. 修复实测问题，测试鼠标双击 clickState、中文 IME、系统快捷键、分辨率变化、休眠唤醒、断线释放按键。
3. Windows Media Foundation 硬解与 D3D11 渲染替换 FFmpeg/WPF 像素路径；测 1080p60 CPU、带宽和输入延迟。再根据证据决定独立视频通道/编码背压优化。
4. 文本剪贴板双向同步（限制大小、去重防回环），Bonjour/mDNS（发现不等于信任，仍须配对）。当前均未实现。
5. 稳定签名与打包、密钥轮换 UI、限速与拒绝服务抵抗、独立安全审计。

始终不做：账号、公网穿透、服务器、TURN/STUN、FileVault 预启动控制、新远程登录 session、移动端。V1 不增加文件传输、多显示器、摄像头、录屏和组织管理。
