# Mac 屏幕访问

[English](../mac-screen-access.md) · [Mac 指南](macos.md)

2026-10-14 的只读诊断确认：alpha.2 下载包本身签名完整、源码版本正确，但系统屏幕授权
匹配的是旧本地编译包，新包从 App Translocation 临时路径运行。TCC 明确报告签名要求不匹配，
因此设置里开启不等于当前包获准访问。这不表示必须购买 Apple 开发者会员才能共享屏幕。

## 现在的流程

macOS 14+ 主动点击“开始接受连接”后，使用 Apple 系统屏幕选择器选择主屏幕。
本次应用运行中缓存授权后的筛选器，并将同一个筛选器交给 ScreenCaptureKit，
不再在选择之后查询 SCShareableContent 而回到旧的全局权限检查。
重连、画质切换复用当前屏幕；停止会关闭监听和采集，再次主动开始可复用本次选择。
退出重开后重新选择。取消/失败不会读取钥匙串、开启监听或自动反复申请。

第一阶段仍是主显示器。macOS 15.2+ 核对系统返回的显示器 ID；14–15.1 仅接受无歧义的单显示器。
macOS 13 保留原屏幕录制授权路径。不修改系统权限数据库或证书信任。
辅助功能是独立权限，仍需允许安装后的应用才能操作键盘鼠标；系统屏幕选择只授权画面。

## 安装下载包

推荐打开 DMG，将 **Local Remote Desktop.app** 拖到 **Applications**。
替换前退出旧实例，之后从“应用程序”打开新版本。ZIP 也可使用，但需先把完整应用包复制到
`/Applications`，避免打开 Downloads 中的应用或旧源码编译副本。
无需本地编译，不自动重置权限，也不关闭 Gatekeeper；未公证包可能仍需正常的首次打开确认。

## 检查范围

记录型服务覆盖拒绝、取消、重复开始、选择前不读取凭据、重试和同次运行复用。
CI 编译原生选择器和完整应用；实际系统选择、画面、输入、权限及升级仍需约定时段验收，
不能把编译成功写成实机通过。

依据 [Apple 系统选择器权限说明](https://developer.apple.com/videos/play/wwdc2023/10053/)、
[ScreenCaptureKit 接入](https://developer.apple.com/videos/play/wwdc2023/10136/)
和[临时签名身份说明](https://developer.apple.com/documentation/technotes/tn3127-inside-code-signing-requirements)。

## 开发中的键鼠权限区分

当前开发版用 CGPreflightPostEventAccess 检查 CGEvent 发送操作，AX 单独记录，
从系统设置返回时刷新；主动允许控制时用 CGRequestPostEventAccess。
若系统未列出当前安装副本，可“定位当前应用”后手动添加。
这不代表 alpha.4 输入故障已实机解决，见[验证范围](../../tests/results/input-access-startup-2026-10-14.md)。
