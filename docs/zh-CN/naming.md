# 工程布局与命名

[English](../naming.md) · [产品名称](product-name.md)

产品名为 **Local Remote Desktop**，仓库名为 `local-remote-desktop`，
工程、程序集、命名空间和 Swift 目标使用 `LocalRemoteDesktop`。
平台目录仍为 `apps/macos`、`apps/windows`，不把平台固定为单一控制/被控角色。
Mac 安装到 `/Applications/Local Remote Desktop.app`，可执行文件为 LocalRemoteDesktop。
Windows 新构建与发布包使用 LocalRemoteDesktop.exe；源码在 Sources/LocalRemoteDesktop 和 LocalRemoteDesktop.csproj。

Mac 安装脚本核对三代名称在系统/用户应用目录中的六个路径，只移除本应用的合法旧包，
停止三种可执行文件名并更新自启动路径。验证失败保留完整恢复副本，不合并新旧文件。
bundle ID、钥匙串服务和 LaunchAgent 标签继续为 com.remotemaclan.host，账户 pairing-v1 不变。
签名/保留权限检查仍执行，更名不保证临时签名更新沿用授权。

Windows 继续使用 `%LOCALAPPDATA%/LocalRemote` 保存加密密码、连接、授权选择、语言和接收文件。
Mac 接收文件目录 LocalRemote/ReceivedFiles 保留；不搬动用户文件。
Bonjour `_localremote._tcp`、RML1/RMP1、旧凭据标记及 LOCALREMOTE_*/RML_* 开关保持兼容。
旧 Windows 启动目录可保留唯一的 LocalRemote.exe 兼容入口，实际加载最新 LocalRemoteDesktop.dll。
规范目录和新包使用 LocalRemoteDesktop.exe，不保留重复产品入口。

已有 v0.1.0/v0.2.0-beta.1 tag、Release 名称、产物和历史验证记录不覆盖；当前仓库链接使用新地址。
