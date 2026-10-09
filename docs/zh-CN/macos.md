# Mac 安装、升级与权限

[English](../macos.md)

要求 Apple Silicon、macOS 13+、正常登录的图形用户会话，以及含 Swift 5.9+ 的 Xcode Command Line Tools。以正常用户执行，不使用 sudo：

```sh
bash macos-host/install-app.sh
```

脚本先构建、验证签名和暂存图标，再停止旧进程及已加载自启动，默认清理本应用屏幕录制和辅助功能授权，完整移除固定位置中已核验的旧应用包，安装并仅启动一个实例；不将旧文件混入新包。构建或图标验证失败会保留旧应用。配对密钥保留；默认安装之后需要重新允许两项权限。

替换期间，完整旧包暂存在私有安装目录中。移动或验证新包失败、收到中断/终止信号时，脚本尝试恢复旧包；成功验证后清除临时副本。恢复失败会保留备份目录并输出路径。这只保护应用文件，不能回滚已重置权限、恢复原进程或应对硬断电。回退后已卸载的登录代理保持停止。若只是自动启动失败，保留已验证的新应用并说明手动打开方式。

使用“屏幕权限”和“辅助功能权限”按钮打开对应设置；辅助功能按钮还会请求本应用访问权限。允许后退出重开，不要为重开再次 install，否则默认安装又会重置两项授权。点击开始共享；辅助功能 false 表示只能查看。显式点击会检查实际 ScreenCaptureKit，即使旧式屏幕预检查为 false。复制启动诊断可查看错误域及代码；构建成功不代表权限成功。

若两项权限已经正常，日常更新可用 `bash macos-host/install-app.sh --keep-permissions` 保留授权。重置只针对 `com.remotemaclan.host`，不影响其他应用，不删除配对密钥。macOS 决定设置列表如何显示，清理授权不保证旧行立即消失，也不能替用户自动授权。参见 [Apple 权限重置说明](https://developer.apple.com/documentation/xcode/resetting-access-to-protected-resources-in-macos)。

## 程序坞图标

应用包同时包含 PNG 和 ICNS，图标声明明确为 `LocalRemote.icns`。启动时主动加载 PNG（ICNS 后备），设置运行中的程序坞图标，登录自启动直接运行内部程序也使用这一路径。安装会验证暂存图标并更新应用目录时间戳，不删除全局图标缓存、不重启程序坞。启动诊断加入 `AppIconLoaded`；诊断脚本输出已安装的图标声明和文件校验值。

## 固定签名

[Apple TN3127](https://developer.apple.com/documentation/technotes/tn3127-inside-code-signing-requirements) 说明临时签名绑定具体代码版本，路径及 bundle ID 不足以保持更新后的身份稳定。在 Mac 配好含私钥的有效 Apple Development 或 Developer ID Application 签名身份后：

```sh
bash macos-host/configure-signing.sh
bash macos-host/install-app.sh
```

脚本只签名验证临时测试文件，保存指纹及稳定模式标记到 Git 忽略的本地文件，不创建证书、不修改信任、不读取配对秘密、不重置权限。后续签名失效即停止，不退回临时签名；首次迁移仍需授权，不能承诺系统永不弹窗。可用 SIGN_IDENTITY 明确指定已有身份。

## 排障

不要反复重装和重置。确认系统权限对应已安装应用，退出重开，使用启动诊断与本机键盘自检。启动时在串行后台队列静默读取一次钥匙串；如果现有密钥需要授权、或首次使用尚无密钥，等待点击“开始共享”或“读取密钥并开始共享”。显式点击后才允许系统认证和首次创建；失败不自动重试。读成功后本次运行复用内存中的密钥。诊断只输出状态和读/创建次数，不输出密钥。

保留现有登录钥匙串项目及访问控制，静默读取时临时禁用交互并在结束后恢复之前设置。参见 [Apple 交互控制说明](https://developer.apple.com/documentation/security/seckeychainsetuserinteractionallowed(_:)) 与 [读取接口的后台执行说明](https://developer.apple.com/documentation/security/secitemcopymatching(_:_:))。文件式钥匙串兼容接口已弃用；此处避免迁移存储，原生 SDK 和系统行为仍需发版验收。临时签名升级依然可能触发信任询问，不能保证以后完全不弹窗或只弹一次。

```sh
bash macos-host/diagnose-permissions.sh
```

默认安装已经重置两项；仍保留兼容的仅屏幕重置选项：

```sh
bash macos-host/install-app.sh --reset-screen-permission
```

它只重置本应用 ScreenCapture 授权，保留辅助功能。`--reset-permissions` 明确执行默认的两项重置；`--keep-permissions` 两项都保留。重置失败会在删除旧应用前停止，之前成功的重置可能已清除某项授权。本机键盘自检仅向自身临时窗口注入，不证明其他应用或安全输入框接受远程事件。

## 登录后启动与边界

手动验证后执行 `bash macos-host/install-launch-agent.sh`。用户 LaunchAgent 在正常登录的 Aqua 会话启动，不会强制重启用户主动退出的程序；屏幕预检查为 false 时仍可能需要点击共享。

取消自启动：

```sh
launchctl bootout gui/$(id -u) ~/Library/LaunchAgents/com.remotemaclan.host.plist
rm ~/Library/LaunchAgents/com.remotemaclan.host.plist
```

不绕过 FileVault 预启动，不创建新会话，不保证登录界面、锁屏或安全框控制。休眠合盖可能不可达，不修改电源策略。

轮换密钥会使所有旧控制端失效：停止退出 Host，执行 `security delete-generic-password -s com.remotemaclan.host -a pairing-v1`，重开并重新配对。不要公开密钥。为兼容保留 `/Applications/RemoteMacLAN.app`、内部程序名及密钥服务名，显示品牌是 LocalRemote。
