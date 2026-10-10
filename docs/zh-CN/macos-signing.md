# Mac 签名与分发计划

[English](../macos-signing.md) · [Mac 指南](macos.md)

当前是 v0.1.1 开发工作，更新防护及启动逻辑测试已实现，尚无独立验收的签名 Mac 发布包。

| 构建 | 用途 | 更新边界 |
|---|---|---|
| 临时签名 | 本地开发 | 代码改变可能改变签名要求；不兼容时拒绝保留权限更新。 |
| Apple Development | 开发者固定 Mac 测试环境 | 固定已有身份，替换前验证签名要求。 |
| Developer ID Application | 下载使用的产品发布 | 由维护者的 Mac 签名、公证及验证产物；用户不应需要自己的证书或本地重编译。 |

依据 [Apple TN3127](https://developer.apple.com/documentation/technotes/tn3127-inside-code-signing-requirements)、
[TN2206](https://developer.apple.com/library/archive/technotes/tn2206/) 及
[公证说明](https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution)。
开发签名迁移到发布签名可能要求重新授权，必须验收。自签名对部分策略可能有效，
但这里未验证 TCC，不将其作为正式分发方案，不自动修改系统信任。

接下来由维护者完成：

1. 准备 Apple Silicon 构建测试机及有效发布身份；私钥、凭据不入库，不进入产物或日志。
2. 执行 `bash tests/MacStartupChecks/check.sh`。Mac 上还会用 Apple SDK 编译完整 Host，不签名、不安装、不打开、不重置 TCC。
3. 正常图形会话中验证首次启动及连续两次签名更新：密钥不变、授权有效，拒绝/取消不会自动重复认证。共享逻辑测试不能证明系统行为。
4. 为最终完整包配置正式发布签名选项、统一签名并公证，在另一 Mac 用户环境验收；附版本、提交、校验值和依赖声明。
5. 通过 AtomGit 在精确提交上发布，旧 tag 和产物不变。后续更新器先验证下载产物和签名兼容性，失败时保留现有应用。

屏幕录制、辅助功能仍需用户主动授权，安装器不能静默授予。稳定签名保持更新身份，但不保证系统永不再次询问。
当前 Windows/Ubuntu 可验证 Windows 客户端及共享 Swift 逻辑；Apple SDK、TCC、钥匙串弹窗、采集、签名公证仍需原生 Mac 验收。

## 维护者打包

`scripts/package-macos.sh` 已实现候选包流程，需要 Apple Silicon Mac、Xcode 工具、
Python 3、已有有效 Developer ID Application 身份及保存在钥匙串中的 notarytool 凭据配置。
由维护者准备，用户不执行此流程；密码、私钥及证书导出不入库。

统一公开版本并提交最终源码后执行：

```bash
export SIGN_IDENTITY='<有效 Developer ID Application 指纹>'
export NOTARY_PROFILE='<已有 notarytool 钥匙串配置名称>'
bash scripts/package-macos.sh 0.1.1
```

示例 0.1.1 是计划版本，当前源码仍是 0.1.0，因此目前请求 0.1.1 会被正确拒绝。
源码必须提交且目录干净，两端版本一致。

流程先完整编译 Host 并执行启动检查，构建全新应用包，用 Hardened Runtime 和安全时间戳签名，
验证 Developer ID 要求并排除调试权限。提交公证并要求 Accepted，附加及校验票据、执行 Gatekeeper 检查，
随后生成最终 ZIP，校验文件清单、版本、Apple Silicon 程序、图标、提交与散列，并解压再次验签及校验票据。
依据 [Apple 公证要求](https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution)
和[自定义流程](https://developer.apple.com/documentation/security/customizing-the-notarization-workflow)。

输出位于 `dist/LocalRemoteDesktop-vVERSION-macos-arm64/`：ZIP、SHA256SUMS.txt、release.json。
包内含依赖声明、中英文启动指引和文件散列，不覆盖已有输出；检查失败清理临时候选包。
流程不安装或打开应用、不重置隐私权限、不读取配对密钥、不改变本地签名固定配置，也不打 tag 或直接发布。

便携测试验证包拒绝规则和模拟 Apple 工具的失败路径，不能代替真实签名、公证或授权验证。
这里尚未原生产生 Mac 候选包；即使打包流程成功，v0.1.1 发布前仍需首次启动及连续两次升级的钥匙串/TCC 实机验收。
