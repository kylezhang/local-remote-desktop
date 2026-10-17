# 屏幕连接台原生实现清单

[English](../screen-desk-implementation.md) · [原生进度](native-ui.md) · [设计预览](../../design/detail-review/desk.html?lang=zh)

2026-10-17。将已评审的屏幕连接台方向对应到两端代码。
**清单已整理，尚未开始原生实现。** 网页截图与测量是设计证据，不是客户端实机验收。
原生进度中的较早条目作为历史记录保留。

## 目标与范围

每个平台一个原生 App，“发起连接 / 接受连接”是平等的两种用途。
围绕当前登录桌面，让记住配对、键鼠、文字和普通文件剪贴板形成顺畅的工作流程。
调整视觉和导航，继续复用现有网络、采集、输入和凭据服务。

首页只出现一组完整屏幕插画，下接两个入口；连接页采用设备展陈；接受页以本机身份为中心，
IP 与密码竖排、底部开始/停止；设置采用紧凑分组；会话以真实屏幕和简短工具栏为主。
不增加笔记功能、虚构桌面应用、账号或再次认证步骤。
中性桌面占位仅供设计预览，客户端显示真实解码画面或明确的连接中/空状态，不能假装已经连接。

## 代码对应与差异

| 区域 | Windows | macOS | 实现内容 |
|---|---|---|---|
| 首页 | MainWindow.xaml、BrandResources.xaml | App.swift 中 DesktopShell、EntryTileStyle、AppBrand | 两张独立插画卡片改为一组完整场景和两个平等入口；一个设置入口；保留活动角色返回入口。 |
| 设备与配对 | MainWindow 设备模板、RebuildDeviceMenu、DeviceConnectClick、ManualDialog | App.swift 中 ConnectionPage、ManualConnectionView；ControllerDiscovery.swift、ControllerMemory.swift | 一个添加目标入口；主动点设备才打开配对；保留草稿、记住/忘记、画质和错误行为。 |
| 接受连接 | Hosting/SharingWindow.xaml 及其代码 | App.swift 中 HostView、SharingActionView；HostModel.swift | 身份、地址、密码竖排，开始/停止固定可达；复用编辑器与生命周期保护。 |
| 权限与失败 | Hosting/SharingFailure.cs、SharingWindow 诊断 | ScreenSharingAccess.swift、MacInputAccess.swift、HostModel.swift | 真实状态驱动恢复操作；屏幕选择与控制授权分开。 |
| 设置 | MainWindow SettingsPage、SaveDefaults | App.swift 中 DesktopSettings；SharingPreferences.swift | 稀疏标签页改为通用、连接、剪贴板、关于四组紧凑列表；保留设置作用范围和原生控件。 |
| 会话 | MainWindow SessionPage、SetSessionLayout、HomeBackClick、ToggleFull | DesktopShell；ControllerView.swift 中 ControllerView、RemoteScreenView | 简短设备/状态栏、选项、原生全屏、明确断开、重连/仅查看反馈和临时传输提示。 |
| 公共资源 | UiTypography.cs、assets/localization/strings.json | UiText.swift、AppBrand、build-app.sh | 复用原生字体与双语目录；原创插画按清晰尺寸打包，不新增运行时渲染依赖。 |

Windows 文件位于 apps/windows，Mac 文件位于 apps/macos/Sources/LocalRemoteDesktop。

“会话中返回首页”需要行为调整。Windows 当前会阻止会话中的返回/导航，Mac 在控制器忙碌时
始终显示 ControllerView。两端都要将“当前页面”和“会话存续”分开：
返回首页或设置不重连、不再次读取凭据；首页有“返回会话”；冲突的角色操作明确提示。
离开远程画面要释放按键与鼠标，画面不再占用键盘焦点。
控制远端与本机正在监听必须有不同状态表达。

设备图片只是通用插画。名称和系统来自发现通告或认证后元数据，不能由 IP 猜测，发现不代表信任。
“已记住”必须对应真实保存的连接记录，缓存通告或密码框非空不足以证明。
如果已有可靠历史，最多突出一台最近成功连接且已记住的设备；否则使用普通网格。
不可达记录可编辑或手动尝试，不能假装在线，也不能自动删掉。
视觉改动中不顺手增加未经设计的历史持久化。

## 原生视觉约束

- 品牌蓝 #5265EC、正文 #202D46、辅助文字 #617088、分隔线 #DFE5EF、设备浅色面 #F0F4FF。
  状态同时有文字。保留品牌、启动文件、稳定 bundle/钥匙串标识和已存凭据。
- 默认外框仍为 600 × 680 逻辑单位，适应工作区，导航不改变用户调整过的大小。
  首批保留现有原生最小尺寸；420 × 568 是预览压力场景，不代表已支持的原生最小尺寸。
- Windows 复用 Segoe UI / 微软雅黑 UI 显式字体映射和受支持的常规字重；Mac 使用系统字体。
  以层级、对齐和密度为准，不能认为 CSS 字号或中间字重能直接复刻到原生。
- 首页重点集中在一组屏幕插画。使用原生支持的矢量或从原创 SVG 确定性导出的多倍资源。
  效果与文字分层，避免字体发虚。
- 保留原生窗口边框与表单习惯。网页底部面板表达任务层级：Windows 用所属窗口内面板，
  Mac 用原生附属 sheet。取消/Escape 正确处理草稿，并恢复触发入口焦点。
- 中英文常规内容在默认窗口内放下。长列表、大字体、错误和小工作区允许内部滚动，
  开始/停止与弹窗操作仍可达；不裁切内容或缩小文字来通过检查。
- 悬停、按下、键盘焦点清楚且克制；适配减少动态效果、对比度/高对比度、读屏名称和键盘顺序。
  不把开关做成位图。

## 实现顺序

| 批次 | 交付内容 | 进入下一批的条件 |
|---|---|---|
| 1：基础与首页 | 公共插画资源/视觉规则；两端新首页与资源打包 | 原生构建、后台双语布局/字体/资源检查通过；生命周期行为保留。 |
| 2：任务页面 | 两端设备展陈与配对面板、接受页和编辑器、紧凑设置 | 草稿/错误/取消、角色冲突回归通过；常规页面无多余滚动，长内容可滚动，停止始终可达。 |
| 3：远程会话 | 导航独立于会话；返回会话、紧凑选项、重连/仅查看、传输提示 | 记录服务证明导航不重连/读凭据，离开释放输入，会话选项受全局允许状态约束，断开正确清理。 |
| 4：候选包 | 对照网页审查原生渲染；对应确定源码的 CI 包与证据 | 两端 CI 与打包检查通过；不替换运行中的客户端；再安排一次集中实机验收。 |

每批都包含两个桌面端，Windows 更新不能代表跨端完成。
此前输入失效、Windows 无法开始接受连接仍是 P0；错误提示好看不等于故障已修复。
如需服务层修正，单独定位并提供回归证据。

## 验证与发版

- [x] 对照原生代码，识别导航和生命周期差异。
- [x] 明确公共层级、原生适配与批次。
- [ ] 完成前三批原生实现。
- [ ] 按变更扩展现有 ClientChecks 的 Shell/Pairing/Sharing/Recovery/SessionInteraction/Typography
  与 MacLayoutChecks；使用记录服务、隔离存储、生成画面，保留协议与输入回归。
  设置改为分组后同步更新测试假设。
- [ ] 审查中英文、默认/受支持最小尺寸、长名称/地址/错误及放大文字的原生渲染。
- [ ] 源码提交 AtomGit，现有镜像触发 GitHub Actions，记录两端成功构建的精确提交、包和报告。
- [ ] 约定时段再集中检查激活状态颜色、焦点、sheet、真实系统授权及双机输入、剪贴板、重连和停止。
  不擅自 SSH、弹前台窗口、读个人剪贴板或注入系统输入。
- [ ] 候选包准备好后选择下一个未使用的 alpha 版本，清单更新本身不改版本号。
  beta/正式版沿用交付门槛，UI 更新或 CI 通过不能清除未解决的 P0。

本轮没有原生构建、权限修改、安装、部署或发布。下一个实现单元是第一批。

