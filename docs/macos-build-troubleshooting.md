# macOS Command Line Tools 构建修正

用户首次 Mac 构建日志显示 SwiftUI.State 的 `SwiftUIMacros.StateMacro` 插件缺失，后续 `$reveal` 不存在是同一错误的连带结果。日志中的 `Capture` Sendable 提示是警告，不是本次阻断构建的错误。日志不含实际配对密钥；不将用户完整日志或本机路径提交。

修正：把显示密钥的状态移到已有 Combine ObservableObject 的 `@Published` 属性，删除 `@State`，无需加载 State 宏。构建脚本使用 `xcrun swift` 和 `--build-system native`，单独使用 `.build-native` 缓存，并通过 `--show-bin-path` 定位产物。

SwiftPM 官方公告确认新版本默认后端改为 Swift Build，仍可显式指定 native：[SwiftPM default build system change](https://forums.swift.org/t/swiftpm-development-update-default-build-system-change/85548)。日志同时显示 Swift Build/Xcode 风格的产物路径；选择 native 是针对构建后端兼容性的处理，不代表已经证明 SDK 安装损坏。

更新源码后在仓库根目录执行：

```bash
bash macos-host/build-app.sh
```

无须删除旧 `.build`。新缓存与旧缓存隔离。构建成功的应用仍位于 `macos-host/build/RemoteMacLAN.app`。

本次 Windows 环境只能检查修改和脚本语法，不能运行 Apple SDK 原生构建。修正针对已知错误；Mac 下一次构建结果才是验证依据。不声称已解决其他尚未出现的 SDK 差异。

## 权限与密钥显示更新

配对密钥现在在应用初始化时从 Keychain 独立加载，不再被屏幕权限检查提前返回挡住。Keychain 读取失败会显示明确错误，不再留下没有说明的空白。显示密钥后可点击复制。未获得屏幕权限仍不能共享桌面。

启动时只检查屏幕权限，不自动反复弹授权框；用户点击开始共享时，本次进程最多主动请求一次授权。无法凭设置中的开关状态推断权限一定通过，应用保留系统权限检查。

构建脚本优先使用显式 SIGN_IDENTITY，其次读取本地 `.signing-identity`，否则从现有 Developer ID Application / Apple Development 证书中选一个并记录，后续复用。此本地配置不提交。没有证书时仍使用 ad-hoc 签名，并明确提示更新后可能需要重授权限；不声称仅固定 bundle ID 就能保证权限保留。稳定的签名要求有助于 macOS 识别更新前后同一应用，参见 [Apple TN3127](https://developer.apple.com/documentation/technotes/tn3127-inside-code-signing-requirements)。证书更换或 macOS 自身周期性授权要求仍可能触发提示。

推荐更新流程，逐行执行：

```bash
git pull
bash macos-host/install-app.sh
```

安装脚本自动执行 build-app.sh，先在 /Applications 临时目录复制并验证新版，再停止已加载的登录自启动与所有同名 Host（包括从构建目录启动的副本），必要时强制结束。仅在显式传入 `--reset-screen-permission` 时执行 `tccutil reset ScreenCapture com.remotemaclan.host`，再完整删除 `/Applications/RemoteMacLAN.app` 和 `~/Applications/RemoteMacLAN.app` 旧包，安装并验证新版，启动一次。旧目标必须是本应用的 bundle ID，且不能是符号链接。构建、签名验证、进程停止或权限重置失败时，会在删除旧包之前中止。

普通安装保留屏幕录制授权；此前每次安装都重置会把刚授予的权限再次清掉，现已修正为按需操作。显式重置后，授予权限，再退出并重新打开 `/Applications/RemoteMacLAN.app`，不要再次安装。辅助功能授权记录及 Keychain 配对密钥不清除。没有保证重置后所有采集问题都会消失。仅共享问题不需要删除密钥。

Windows 上已执行 Bash 语法检查及隔离的安装流程测试（模拟 macOS 命令）：覆盖旧包全部文件及用户目录副本删除、强制停止、登录自启动恢复且不重复 open、指定 TCC 命令，以及构建/签名/身份/进程/权限重置失败时保留旧包。测试不会运行真实 tccutil 或 launchctl；实际 macOS 行为仍未在本开发环境验证。测试入口为 `tests/InstallerChecks/check.py`，需 Python 3.10+ 和 Bash。

若权限仍无法通过：运行 `bash macos-host/diagnose-permissions.sh`，检查安装包签名要求及可用签名身份。该脚本不读取配对密钥；不能在 Windows 环境代替实际 Mac 诊断。不要提供真实密码或配对密钥。

## 0.2.1 启动检查修正

根据用户提供的日志，安装执行两次，两次均明确重置了 ScreenCapture 授权；日志只能证明安装和签名验证通过，不能证明当前进程的屏幕访问被允许。旧程序还在 CGPreflightScreenCaptureAccess 为 false 时直接退出启动流程，无法区分旧预检查和 ScreenCaptureKit 实际结果。

0.2.1 保留正常安装时的授权，并在主动点击开始共享时异步调用 SCShareableContent，成功后才监听 TCP 47980。不会绕过系统授权。界面显示检查中、等待 Windows 或系统 NSError 域/码；新增“复制启动诊断”，提供系统版本、预检查、实际 SCK 检查、辅助功能状态和监听状态，不含密钥、序列号或用户目录。补充 NSScreenCaptureUsageDescription，参见 [Apple ScreenCaptureKit](https://developer.apple.com/documentation/screencapturekit)。

Windows 上 Bash 语法及 5 组隔离模拟安装测试通过，包括默认不重置与显式重置。Swift 静态语法解析通过；没有 macOS SDK 编译或 macOS 27 实机验证，不宣称兼容性或此故障已全部解决。
