# 发布检查点

[English](../next-release.md) · [路线图](roadmap.md)

2026-10-10：**v0.2.0-beta.1 测试版已发布**到 [AtomGit](https://atomgit.com/zkxw2008/local-remote-desktop/releases)。两端显示测试版标识；Mac 数字版本 0.2.0、内部构建 19。
v0.1.0 tag 和附件保持不变。本次增加密码/剪贴板功能，因此原 v0.1.1/v0.1.2 补丁计划并入次版本测试版。

## 测试版交付

- [x] Windows 含 .NET 运行时/说明文件的安装包及准确启动指南。
- [x] 包内程序集通过旧密钥/短密码两模式 WPF/加密模拟端回归。
- [x] 手填/短密码、快捷键、文字/文件、紧凑菜单和 Mac 独立密码编辑窗口。
- [x] 便携 Swift 凭据/启动/实际 HostModel 检查，不声明原生 SDK 通过。
- [x] Windows 合成解码两档各 120 秒及清理检查，保留原始采样。
- [x] Mac 包检查拒绝将 beta 元数据当正式二进制，隔离模拟通过。
- [x] 双语范围、限制、更新日志及版本标识同步。
- [x] 从发版提交重建并回归最终包，核验 ZIP/校验值。
- [x] AtomGit 注释 tag、预发布、Windows ZIP/校验附件及下载核验。

仅交付已测试 Windows 包与 **Mac 源码**，不发布未验证 Mac 二进制。
见[发布说明](../release-v0.2.0-beta.1.md)和[交付核验记录](../../tests/results/release-v0.2.0-beta.1-2026-10-10.txt)。

## 正式 v0.2.0 门槛

已验证本地 beta.2 候选的发现/语言功能，见[语言记录](../../tests/results/localization-2026-10-10.txt)。
主分支推进到 **v0.3.0-alpha.3 开发版**，Mac 数字版本 0.3.0/内部构建 28，
包含 Windows 共享角色与 [Mac 控制端源码预览](mac-controller.md)。
便携 Swift 控制端规则、状态、会话及独立加密端检查通过；Apple SDK 原生构建与 VideoToolbox 样例解码现已通过；安装后界面/输入及真实 Mac → Windows 验收仍待完成。见[当前交付计划](v0.3.0-plan.md)。
尚未创建新 alpha release/tag 或 Mac 二进制包；下列原生验收门槛仍有效。

- [x] Apple SDK 正式构建及 VideoToolbox 色块解码。
- [ ] 安装后的真实 Mac 界面/密码编辑及原生输入验收。
- [ ] 钥匙串授权/拒绝/重试、保存、重启持久化及升级行为。
- [ ] 实际安装签名、权限保留/清理、图标/自启动与恢复测试。
- [ ] 真实 Windows → Mac 键鼠、文字/文件、允许/撤销、重连与休眠唤醒回归。
- [ ] 更长的完整会话资源/延迟检查；当前解码采样不是长时间完整远控测试。
- [ ] 自动发现及中英文界面，或明确修订正式版功能范围。
- [ ] 如提供 Mac 二进制：ZIP/DMG 解压验证、明确临时签名/未公证说明及原生安装升级验收；Developer ID/公证可选。
- [ ] 正式版本、最终包、校验文件、说明及不可变 tag。

已通过 SSH 在隔离检出目录完成 Mac 原生构建与样例检查，没有更改已安装应用或其权限。正式日期取决于剩余验收，反复跑模拟不能替代；
不将钥匙串重复认证/TCC 问题宣称永久解决。测试版只是当前实现快照。

## 当前 v0.3.0 验收

上方 Apple SDK/VideoToolbox 通过项属于较早源码检查点。随后 alpha.2 已通过[云端原生编译和后台检查](../../tests/results/github-actions-2026-10-13.md)，用户反馈 Mac → Windows 控制成功。Mac 只读诊断确认下载包接受连接时，全局屏幕授权仍匹配旧代码身份。alpha.3 加入[系统屏幕选择](mac-screen-access.md)和 DMG 安装，原生选择器/升级验收仍待完成。用户工作时不通过 SSH 发起前台测试。使用[集中清单](desktop-acceptance.md)，不宣称新的正式版已完成。
