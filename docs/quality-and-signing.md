# Windows 0.2.3 与固定 Mac 签名

已能正常使用的 Mac Host 可保持现状。连接中切画质仅需更新 Windows 客户端，不改协议，也不需要重装 Mac。

## 权限为何在更新后失效

Apple 的 [TN3127](https://developer.apple.com/documentation/technotes/tn3127-inside-code-signing-requirements) 说明 ad-hoc 签名的 designated requirement 绑定具体代码版本。固定安装路径和 bundle ID 无法单独解决此身份变化；需要复用有效证书的签名身份，同时保持 bundle ID 和安装位置一致。签名身份稳定也不保证系统权限或钥匙串永远不会再弹窗。

用户此前诊断为零有效签名身份，所以仅拉代码不能自动得到有效 Apple 证书。先在 Mac 配置含私钥的 Apple Development 或 Developer ID Application 身份，再运行：

```bash
bash macos-host/configure-signing.sh
bash macos-host/install-app.sh
```

配置脚本只查询已有签名身份、签名并验证临时测试文件，成功后将指纹与稳定模式标记保存在被 Git 忽略的本地文件。它不创建证书、不改变系统信任或重置 TCC。构建复用固定身份，签名失效即中止，不退回 ad-hoc；从 ad-hoc 迁移的首次安装仍需重新授权。正常 install 不重置屏幕权限，不要在每次启动时重复 install 或权限 reset。没有证书时仍允许开发构建，但会说明权限风险。

## 验证范围

Windows 0.2.3 实际 Release 构建及 win-x64 self-contained 发布成功。真实 WPF 窗口和 FFmpeg 解码器连接到加密协议模拟 Host，验证画面、键盘/鼠标、权限状态、失焦释放、断开清理；新增验证连接中从流畅切到标准后重新认证并显示新画面，快速切高清再流畅采用最后档位，且关闭自动重连仍生效。

签名脚本在隔离目录中使用模拟 macOS 工具检查六个情形：无证书配置失败、有效证书配置成功、稳定模式缺证书在构建前失败、已保存身份失效不退回临时签名、证书构建保存并固定身份、开发模式明确允许 ad-hoc。这验证流程，不验证真实 macOS 签名或 TCC。当前环境不能原生构建 Mac，也没有实际跨 Windows/Mac 的端到端测试；不能据此保证锁屏输入或高画质性能。
