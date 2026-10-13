# Mac 安装、升级与权限

[English](../macos.md)

要求 Apple Silicon、macOS 13+、正常登录的图形用户会话，以及含 Swift 5.9+ 的 Xcode Command Line Tools。以正常用户执行，不使用 sudo：

```sh
bash apps/macos/install-app.sh
```

构建会在 Git 忽略的 `.build-native` 缓存中记录项目实际路径。移动或重命名项目后，
以及首次遇到旧版未标记缓存时，会自动清理本项目这份缓存并重新编译，避免
`SwiftShims`/Clang 模块仍引用旧路径；之后同路径构建继续复用缓存。缓存路径为符号
链接时拒绝操作。此清理不重置权限、配对密码或签名配置；构建失败仍保留已安装应用。

脚本先构建、验证签名和暂存图标，再停止旧进程及已加载自启动，签名兼容检查通过后保留本应用屏幕录制和辅助功能授权，完整移除固定位置中已核验的旧应用包，安装并仅启动一个实例；不将旧文件混入新包。构建或图标验证失败会保留旧应用。配对密码保留；首次安装或显式权限修复仍需允许相应权限；正常升级不主动重置。

新位置为 `/Applications/Local Remote Desktop.app`。安装器也识别系统/用户目录中的旧 `RemoteMacLAN.app`，验证成功后移除，失败时可恢复；已有登录自启动更新到新程序路径。

替换期间，完整旧包暂存在私有安装目录中。移动或验证新包失败、收到中断/终止信号时，脚本尝试恢复旧包；成功验证后清除临时副本。恢复失败会保留备份目录并输出路径。这只保护应用文件，不能回滚已重置权限、恢复原进程或应对硬断电。回退后已卸载的登录代理保持停止。若只是自动启动失败，保留已验证的新应用并说明手动打开方式。

使用“屏幕权限”和“辅助功能权限”按钮打开对应设置；辅助功能按钮还会请求本应用访问权限。允许后退出重开，不要为重开再次 install，重新打开不需要重建或修复权限。点击开始共享；辅助功能 false 表示只能查看。显式点击会检查实际 ScreenCaptureKit，即使旧式屏幕预检查为 false。复制启动诊断可查看错误域及代码；构建成功不代表权限成功。

正常安装默认使用保留权限路径，`--keep-permissions` 是显式别名。停止旧程序、改变权限之前，脚本比较新包与每份已安装应用的 designated requirement，并按相应要求验证双方签名。要求不同、无法读取或验签失败即中止，现有运行程序、应用包及权限均不改动。临时签名更新通常无法通过；这个选项不能解决签名身份不稳定。签名要求相同也不保证 macOS 永久保留所有权限。

重置只针对 `com.remotemaclan.host`，不影响其他应用，不删除配对密码。macOS 决定设置列表如何显示，清理授权不保证旧行立即消失，也不能替用户自动授权。参见 [Apple 权限重置说明](https://developer.apple.com/documentation/xcode/resetting-access-to-protected-resources-in-macos)。

## 程序坞图标

应用包同时包含 PNG 和 ICNS，图标声明明确为 `LocalRemoteDesktop.icns`。启动时主动加载 PNG（ICNS 后备），设置运行中的程序坞图标，登录自启动直接运行内部程序也使用这一路径。安装会验证暂存图标并更新应用目录时间戳，不删除全局图标缓存、不重启程序坞。启动诊断加入 `AppIconLoaded`；诊断脚本输出已安装的图标声明和文件校验值。

## 固定签名

[Apple TN3127](https://developer.apple.com/documentation/technotes/tn3127-inside-code-signing-requirements) 说明临时签名绑定具体代码版本，路径及 bundle ID 不足以保持更新后的身份稳定。在 Mac 配好含私钥的有效 Apple Development 或 Developer ID Application 签名身份后：

```sh
bash apps/macos/configure-signing.sh
# 从临时签名首次迁移，需显式选择权限修复。
bash apps/macos/install-app.sh --reset-permissions
```

脚本只签名验证临时测试文件，保存指纹及稳定模式标记到 Git 忽略的本地文件，不创建证书、不修改信任、不读取配对秘密、不重置权限。后续签名失效即停止，不退回临时签名；首次迁移仍需授权，不能承诺系统永不弹窗。可用 SIGN_IDENTITY 明确指定已有身份。

## 排障

临时签名或未标记构建启动不访问钥匙串。“开始共享”先检查屏幕访问，失败时不读取密钥；证书构建自动启动也先检查屏幕。后台读取失败等待用户操作，不自动转为允许弹窗的读取。包内签名模式只用于启动策略，不代表证书信任验证。诊断加入模式、内部构建号、后端及读取/创建次数，不输出密钥。

需要在屏幕未授权时单独查看密码，点击“当前密码”旁的“查看”。此显式操作可能要求认证，但不启动共享、不改变共享状态；之后查看和共享复用本次运行已读取的凭据。“复制”使用原始密码，不含显示时的换行。

显式点击才允许系统认证及首次创建，失败不自动重试；成功后本次运行复用密钥。因此开发构建登录后仅打开，不自动开始共享；证书构建只有无需交互读取密钥且屏幕授权有效时才可自动共享。

查询明确选择原文件式钥匙串（`kSecUseDataProtectionKeychain=false`），保留服务名、账户及访问控制。[Apple 桥接源码](https://github.com/apple-oss-distributions/Security/blob/main/OSX/libsecurity_keychain/lib/SecItem.cpp) 显示，未指定后端可能查询两种存储，兼容旧式存储的新增优先进入文件式后端。此处消除路由歧义，不能据此断言两次弹窗的原因；不迁移存储、不回退到现代后端。

证书构建静默读取临时禁用旧式交互并恢复原设置，不依赖现代查询级 UI 参数控制文件式后端。参见 [Apple 交互控制说明](https://developer.apple.com/documentation/security/seckeychainsetuserinteractionallowed(_:)) 与 [后台执行说明](https://developer.apple.com/documentation/security/secitemcopymatching(_:_:))。这些兼容接口已弃用；连续两次弹窗来源、新实现系统行为未在真实 Mac 复现，不能算已解决。不引入明文存储、自动换密钥或放宽访问控制。

产品使用的启动顺序、缓存和取消逻辑由 [MacStartupChecks](../../tests/MacStartupChecks/check.sh) 实际编译执行。Linux 测试不验证 Apple 框架或系统弹窗。长期更新路径见[签名与分发计划](macos-signing.md)。

```sh
bash apps/macos/diagnose-permissions.sh
```

确需修复时使用 `bash apps/macos/install-app.sh --reset-permissions` 重置两项；仍保留兼容的仅屏幕重置选项：

```sh
bash apps/macos/install-app.sh --reset-screen-permission
```

它只重置本应用 ScreenCapture 授权，保留辅助功能。`--reset-permissions` 明确执行两项修复重置；`--keep-permissions` 通过签名兼容检查后才跳过两项重置。重置失败会在删除旧应用前停止，之前成功的重置可能已清除某项授权。本机键盘自检仅向自身临时窗口注入，不证明其他应用或安全输入框接受远程事件。

## 登录后启动与边界

手动验证后执行 `bash apps/macos/install-launch-agent.sh`。用户 LaunchAgent 在正常登录的 Aqua 会话启动，不会强制重启用户主动退出的程序；屏幕预检查为 false 时仍可能需要点击共享。

取消自启动：

```sh
launchctl bootout gui/$(id -u) ~/Library/LaunchAgents/com.remotemaclan.host.plist
rm ~/Library/LaunchAgents/com.remotemaclan.host.plist
```

不绕过 FileVault 预启动，不创建新会话，不保证登录界面、锁屏或安全框控制。休眠合盖可能不可达，不修改电源策略。

开发版本在“当前密码 → 修改密码 → 保存密码”中更换：手填 8 位密码或自动生成 8 位草稿，保存才生效。保存更新原有记录并断开所有当前控制端，在已更新的 Windows 填新密码即可。旧密钥保留到主动保存。正常改密码无需删除钥匙串项，不要公开密码。程序文件改为 Local Remote Desktop，系统授权、钥匙串和自启动身份保持稳定，见[命名迁移](naming.md)。

## SDK 27 命令行工具兼容

2026-10-13 已在只有 Command Line Tools 的 macOS 27 SDK / Apple Swift 6.4 环境完成原生构建。
此 SDK 将 @State 暴露为宏，测试工具中缺少 SwiftUIMacros 插件。应用的 ViewState 别名改用
原有 SwiftUI State 属性包装器，保留 macOS 13 基线。构建前更新源码即可；缺少编译宏与权限
无关，不应重装应用或重置 TCC。构建成功不等于安装界面、钥匙串授权或真实输入验收通过。
