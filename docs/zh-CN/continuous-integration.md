# 在线构建

[English](../continuous-integration.md) · [开发手册](development-handbook.md)

GitHub Actions 是主要编译和自动测试环境，本地编译用于开发时按需使用。
AtomGit 保持源码主仓库，现有流程将提交同步到
[GitHub 镜像](https://github.com/kylezhang/local-remote-desktop)。工作流直接构建镜像提交，不管理同步过程。

## 触发与下载

GitHub 收到 `main` 更新、`v*` 标签或面向 `main` 的 PR 时运行 `Desktop CI`；
也可进入 **Actions → Desktop CI → Run workflow** 手动运行。
新的分支提交取消旧的未完成构建，标签构建不被后续提交取消。
流程只需仓库读取权限，Action 固定到源码提交，开发构建无需额外凭据；镜像需启用 Actions。

Windows 使用 `windows-2022`、.NET SDK 8.0.425 和校验值固定的 FFmpeg 7.1.1 测试依赖；
Mac 使用 Apple Silicon `macos-15` 及 Apple SDK，记录 Swift/Xcode 版本。
不跨项目路径复用原生编译缓存。Ubuntu 负责源码和安装/打包模拟检查。

1. 打开[构建记录](https://github.com/kylezhang/local-remote-desktop/actions/workflows/ci.yml)，选择成功的运行并核对提交。
2. 在 **Artifacts** 下载 `windows-x64-<提交>` 或 `macos-arm64-preview-<提交>`。
3. 解压外层产物，再解压其中的应用 ZIP。
4. 外层 `SHA256SUMS.txt`、`ci.json` 标明校验值和检查范围；应用包内的 `SOURCE.txt`、
   `source.zip` 提供对应 GPL 源码。

产物保存 14 天，下载需要登录 GitHub 并有仓库读取权限；过期后可重新构建指定提交。
正式版本下载仍使用独立的不可覆盖发行包。

Windows 包含 .NET Desktop Runtime。**FFmpeg 暂时仍需外部提供**，可复用原有完整安装，
或在 PATH/应用旁提供支持 H.264 的完整版本。CI 固定 FFmpeg 只用于生成画面的测试，不随应用分发；
后续打包 FFmpeg 需补齐选定第三方构建的完整对应源码和许可。

Mac 提供完整 `.app`、图标、语言及插画资源，是**临时签名、未公证的开发预览包**。
系统可能阻止下载包打开，更新后也可能需要重新授权或访问钥匙串。请按包内说明操作，
不要关闭 Gatekeeper。正式分发需[固定 Developer ID 与公证](macos-signing.md)。
打包过程不安装/启动应用、不重置权限、不读取用户配对密钥。

## 检查范围

- Windows 原生编译、协议/认证、隔离 DPAPI、文字/文件、生命周期、模拟鼠标坐标及不显示窗口的 WPF 流程。
  客户端检查直接使用最终包中的程序集。
- 各画质的生成画面、软件 H.264 编码、加密回环传输和解码；不宣称云端验证了硬件编码性能。
- 原生 Swift 样例、完整 Apple SDK 编译、中英文不显示窗口的布局检查、资源封装和解压后预览签名验证。
- 版本、源码/许可、翻译覆盖及模拟安装/正式打包控制流程。

真实屏幕、鼠标键盘、TCC/钥匙串确认、实际剪贴板与局域网、硬件性能、长时间会话明确记录为未执行。
云端没有有效显示器时，物理显示器检查单独报告，不影响模拟坐标覆盖，也不算实机通过。
绿色 CI 不代替[桌面验收](desktop-acceptance.md)。

## 开发与发版

修改代码并提交 AtomGit，查看 GitHub 自动构建结果，再根据报告修复；本地编译可选。
Windows 可在当前客户端退出后使用验证过的 Actions 包，不覆盖运行中的应用或为部署修改个人权限。

`scripts/ci/` 是统一入口。`LOCALREMOTE_CI_CHECKS=1` 使用 Windows 软件编码和生成画面；
`LOCALREMOTE_SKIP_NATIVE_BUILD=1` 只省去 Swift 样例之间重复的完整编译，Mac 打包步骤仍编译整个应用。
这两个开关均不表示原生实机验收通过。

当前标签只生成候选包，不自动创建 GitHub/AtomGit Release，既有标签与发行包保持不变。
正式 Mac 打包仍严格要求 Developer ID、公证和稳定版本，不退回临时签名；
后续签名发版自动化使用受保护的凭据，并在验收通过后发布。
