# 普通锁屏与数字键

普通锁屏与 FileVault Preboot 不同：前者仍有已登录用户会话和运行中的 Host。当前用户实测反馈锁屏密码框能收到字母及字符，因此该目标 Mac 的输入链路在锁屏后仍运行。不能据此保证所有 macOS 版本/安全配置都允许锁屏捕获及注入，也不能推论重启未登录时可控制。Mac 休眠或合盖睡眠会阻断连接。

数字键故障排查：

1. 区分字母上方数字行和右侧数字小键盘。小键盘先确认 Windows Num Lock 开启。
2. 原 Host 的小键盘 key code 已映射，但覆盖 CGEvent.flags 时未保留 numeric-pad 标记。已补齐，是否为用户现场故障根因仍需复测；不是已经确认修好锁屏输入。
3. 更新并重编译 Mac Host，退出旧 Host 后替换 /Applications 的应用并重新打开。Windows 端无需变更。
4. 用无敏感内容的 TextEdit 测试两组 0..9，再测试锁屏。不要提供真实密码、按键录制或密码框截图。

macOS 数字小键盘标记依据：[Apple CGEventFlags.maskNumericPad](https://developer.apple.com/documentation/coregraphics/cgeventflags/masknumericpad)。Num Lock 关闭时 Windows 导航键仍保持导航含义，不强行转换为数字。
