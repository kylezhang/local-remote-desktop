# 对外推广计划

[English](../launch-plan.md) · [桌面就绪标准](v0.3.0-plan.md)

整理于 **2026-10-14**。这是方案和物料清单，不代表已获发帖、联系社区、购买推广或宣称正式版的授权。

## 定位

**在局域网中，连接并操作你的 Windows 或 Mac。**

主线是 Windows/Mac 混用的实际需求：直连当前会话，每平台一个应用，记住配对、文字/文件，
无需账号或云中转。AI 辅助开发放在创作故事里，不代替产品价值。
准确使用开源/GPL-3.0-or-later，不贬低付费产品，也不宣称极致延迟、已审计安全或 Mac 永不重授权。

当前为 **v0.3.0-alpha.3**，原生构建及自动发行通过，完整安装后权限、升级、剪贴板及持续使用仍待验收。
移动端/Linux/HarmonyOS 仅为路线目标。

## 仓库入口

GitHub API 核对结果：简介/网址为空，Topics 为空；Issues 开启，Discussions 未开启。
代码镜像不代表这些设置已完善。

拟填写 GitHub About：

- 英文简介：**Open-source remote desktop for Windows and macOS over a local network. Direct connections, no accounts or cloud relay. GPL-3.0-or-later.**
- 真正官网上线前，网址为 **https://github.com/kylezhang/local-remote-desktop/releases**。
- Topics：**remote-desktop、remote-control、lan、windows、macos、peer-to-peer、swift、wpf、dotnet、clipboard、file-transfer**。

这些已备好，**尚未实际写入**：当前连接没有仓库设置写入工具，浏览器绑定超时。
不索取或复制 Token，不修改源码镜像流程。

已新增 .github/ISSUE_TEMPLATE 问题/建议表单，现有镜像收到提交后生效。
初期集中到一个 GitHub 反馈入口，先不铺多个无人维护论坛。
扩大推广前确认私密漏洞反馈渠道，不能让安全细节进入公开问题表单。

## 推广前的物料

| 物料 | 用途 | 就绪标准 |
|---|---|---|
| 中英文项目介绍 | 说明价值、支持和下载 | 对应当前包，准确说明前提及 alpha 限制。 |
| 1280×640、1 MB 内分享 PNG | 仓库链接展开可识别 | 沿用已确认 logo，一句话价值，准确 Windows/Mac 支持，不虚构官网或平台。 |
| 三张真实截图 | 首页、接受连接、远控现场 | 专用去敏会话、实际 App/版本；设计稿标明，不能冒充实测。 |
| 30–60 秒演示 | 完整日常流程 | 连接、输入/点击、文字/文件粘贴、断开；两方向须实际录制和验证后展示。 |
| 安装/FAQ | 减少首次试用阻力 | 说明外部 FFmpeg、Mac 首次打开/选择器/辅助功能、已有 VPN/手填 IP，不要求关闭保护。 |
| 分渠道文案 | 有价值地介绍项目 | 双语创作故事、可用下载、限制、具体反馈问题，发布前用户审阅。 |

实测暂停时不打开/录制用户桌面准备物料；真实截图和演示等约定时段。
大范围曝光前优先处理 FFmpeg 依赖及 Mac 授权的安装阻力；是否捆绑依赖需另行决定打包与许可。

## 分阶段执行

1. **现在完善仓库**：取得可用管理入口后填写 About；补反馈表单、下载/FAQ 及视觉物料计划。
   同时继续修复/原生 CI；改文档不表示升级应用或发行新包。
2. **小范围邀请试用**：目标先 10–20 位 Windows/Mac 混用志愿用户，注明 alpha 和前提，
   少数合适的国内外技术社区先积累可复现首次体验，再扩大。邀请/发帖需明确授权。
3. **桌面 beta 推广**：P0/基础两方向验收后，制作真实演示及分渠道内容。
   国内考虑 V2EX、技术文章/视频；国际考虑 Show HN、英文开发故事。
   发帖前核对当时自荐规则，不批量群发，不要求刷票/Star；Show HN 要求用户能实际试用。
4. **扩大桌面发布**：完整桌面验收后考虑 Product Hunt、远程办公/开源社区及软件目录。
   先有清晰产品/下载页和答疑能力，初期不建议付费投放。

GitHub 作为国际代码/下载入口，AtomGit 作为主源码与国内下载入口；以后可有静态介绍页导流，
它不是桌面中转服务器。本计划不自动授权部署网站。

## 观察效果

看可取得的聚合访问、下载、用户自愿反馈的首次连通/再次使用、可复现问题及修复时间。
Star 次要，不因推广增加 App 遥测。对安全和未支持平台的提问准确回应。

下一步：补可访问的元信息、分享卡/文案、首次安装说明，形成完整物料包供审阅。
真实演示与实机验收仍暂停，明确恢复后再安排。

依据：[GitHub Topics](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/classifying-your-repository-with-topics)、
[分享预览](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/customizing-your-repositorys-social-media-preview)、
[Show HN 规则](https://news.ycombinator.com/showhn.html)、
[Product Hunt 准备指南](https://www.producthunt.com/launch/preparing-for-launch)。
