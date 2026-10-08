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
bash macos-host/build-app.sh
bash macos-host/install-app.sh
```

安装脚本正常退出正在运行的 /Applications Host，复制新包、验证签名并打开，避免覆盖运行中的旧进程。不会重置隐私数据库或删除配对密钥。

若权限仍无法通过：运行 `bash macos-host/diagnose-permissions.sh`，检查安装包签名要求及可用签名身份。该脚本不读取配对密钥；不能在 Windows 环境代替实际 Mac 诊断。不要提供真实密码或配对密钥。
