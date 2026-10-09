# 工程结构与命名

[English](../naming.md)

产品、仓库、应用、源码命名空间统一为 LocalRemote。源码位于 `apps/macos` 和
`apps/windows`，不再用目录名长期限定控制端/被控端；目前仍只实现 Windows→Mac。
共享图标在 assets/brand，英文及中文文档在 docs，通信约定在 docs/protocol.md，工具在 scripts，测试在 tests。

Mac 产物为 `LocalRemote.app`，内部程序 `Contents/MacOS/LocalRemote`，安装到
`/Applications/LocalRemote.app`。Windows 工程为 `LocalRemote.csproj`，程序及程序集为
`LocalRemote.exe`、`LocalRemote.dll`。构建、打包和测试引用同步更新。

## 更新兼容

| 保留标识 | 原因 |
|---|---|
| Mac bundle ID `com.remotemaclan.host` | 沿用签名与系统授权身份，改变需另做迁移。 |
| 钥匙串服务 `com.remotemaclan.host`、账户 `pairing-v1` | 保留原配对密钥。 |
| LaunchAgent label `com.remotemaclan.host` | 维持唯一登录任务，只更新程序路径。 |
| 通信 magic `RML1` 及已有加密标签 | 新旧版本仍可互联。 |

以上是稳定内部约定，显示名称、源码和发布文件均使用 LocalRemote，不为清空搜索结果而破坏兼容。

Mac 安装器验证系统/用户目录中的新旧四个应用位置，替换前保留完整备份。
旧 RemoteMacLAN.app 在成功更新后移除，验证失败可恢复；新旧进程名均会停止。
配对存储保留，自启动路径在新包验证后更新；保留/重置权限政策不变，改名不保证系统继续认可授权。

Git 不迁移忽略的本地签名配置。开发构建会在新目录缺失时复制旧签名固定配置和严格模式标记，
不覆盖新配置，发布构建不改变这些设置；旧 RML 签名环境变量保留为兼容别名。

Windows 从 `%LOCALAPPDATA%/RemoteMacLAN` 一次性复制已识别的 DPAPI 加密记录到
`%LOCALAPPDATA%/LocalRemote`，不重新加解密、不覆盖新记录、不删原文件。
完成标记防止忘记的连接被重新迁入；旧本地启动快捷路径可保留兼容部署副本，正式包使用新名称。

v0.1.0 tag、下载附件和历史测试记录保留当时名称；本次改名进入后续版本，不改写已发布历史。
