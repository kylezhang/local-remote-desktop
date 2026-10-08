# Mac 安装、升级与权限

[English](../macos.md)

要求 Apple Silicon、macOS 13+、正常登录的图形用户会话，以及含 Swift 5.9+ 的 Xcode Command Line Tools。以正常用户执行，不使用 sudo：

```sh
bash macos-host/install-app.sh
```

脚本先构建和验证新包，再停止旧进程及已加载自启动，完整删除固定位置中已核验的旧应用包，安装并仅启动一个实例。构建失败保留已安装应用。保留配对密钥，默认不重置权限；临时签名代码变化仍可能要求重新授权。

在隐私与安全性中授权屏幕录制和辅助功能。授权后退出重开，不要为重开再次 install。点击开始共享；辅助功能 false 表示只能查看。显式点击会检查实际 ScreenCaptureKit，即使旧式屏幕预检查为 false。复制启动诊断可查看错误域及代码；构建成功不代表权限成功。

## 固定签名

[Apple TN3127](https://developer.apple.com/documentation/technotes/tn3127-inside-code-signing-requirements) 说明临时签名绑定具体代码版本，路径及 bundle ID 不足以保持更新后的身份稳定。在 Mac 配好含私钥的有效 Apple Development 或 Developer ID Application 签名身份后：

```sh
bash macos-host/configure-signing.sh
bash macos-host/install-app.sh
```

脚本只签名验证临时测试文件，保存指纹及稳定模式标记到 Git 忽略的本地文件，不创建证书、不修改信任、不读取配对秘密、不重置权限。后续签名失效即停止，不退回临时签名；首次迁移仍需授权，不能承诺系统永不弹窗。可用 SIGN_IDENTITY 明确指定已有身份。

## 排障

不要反复重装和重置。确认系统权限对应已安装应用，退出重开，使用启动诊断与本机键盘自检。密钥每进程启动只尝试读一次，失败后仅通过按钮重试；临时签名更新仍可能触发钥匙串信任询问。

```sh
bash macos-host/diagnose-permissions.sh
```

仅确认旧屏幕权限记录失效时使用：

```sh
bash macos-host/install-app.sh --reset-screen-permission
```

它只重置本应用 ScreenCapture 授权，不重置其他应用或辅助功能；之后必须重新授权。本机键盘自检仅向自身临时窗口注入，不证明其他应用或安全输入框接受远程事件。

## 登录后启动与边界

手动验证后执行 `bash macos-host/install-launch-agent.sh`。用户 LaunchAgent 在正常登录的 Aqua 会话启动，不会强制重启用户主动退出的程序；屏幕预检查为 false 时仍可能需要点击共享。

取消自启动：

```sh
launchctl bootout gui/$(id -u) ~/Library/LaunchAgents/com.remotemaclan.host.plist
rm ~/Library/LaunchAgents/com.remotemaclan.host.plist
```

不绕过 FileVault 预启动，不创建新会话，不保证登录界面、锁屏或安全框控制。休眠合盖可能不可达，不修改电源策略。

轮换密钥会使所有旧控制端失效：停止退出 Host，执行 `security delete-generic-password -s com.remotemaclan.host -a pairing-v1`，重开并重新配对。不要公开密钥。为兼容保留 `/Applications/RemoteMacLAN.app`、内部程序名及密钥服务名，显示品牌是 LocalRemote。
