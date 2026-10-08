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
