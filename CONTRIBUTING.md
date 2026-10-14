# Maintaining Local Remote Desktop

Start with the [development handbook](docs/development-handbook.md) / [开发约定与经验](docs/zh-CN/development-handbook.md).
It records product decisions, shared UI flows, update/permission lessons and validation rules for every platform.

For installation/control problems or feature ideas, use the [GitHub feedback forms](https://github.com/kylezhang/local-remote-desktop/issues/new/choose).
Include the released app version, both operating systems, connection direction and
reproducible steps. Remove passwords, tokens, private addresses and personal content
from diagnostics/screenshots. English and Chinese feedback are welcome.
Public outreach preparation is tracked in the [launch plan](docs/launch-plan.md).

中文：安装/远控问题和功能建议可使用上述 GitHub 表单，注明发行版本、两端系统、连接方向与复现步骤，
去掉密码、令牌、私人地址及个人内容。中英文均可；对外物料见[推广计划](docs/zh-CN/launch-plan.md)。

Use small, focused commits with Conventional Commit-style subjects: `feat:`, `fix:`, `docs:`, `test:`, `chore:`. Describe user-visible behavior and validation; do not claim platform testing that was not performed. Keep README/roadmap statements aligned with implemented support. English is the primary documentation; update corresponding Chinese pages when changing behavior.

Do not commit build output, runtime binaries, release archives, credentials, pairing keys, logs, private certificates or user connection data. Check both `.gitignore` and already-tracked files: ignore rules do not untrack existing files. Avoid broad patterns that hide required icons, test fixtures or source assets. `.gitattributes` keeps shell scripts and source files on LF across platforms.

For changes to transport/input, run ProtocolChecks and ClientChecks on Windows with FFmpeg. For installer/signing changes, run isolated mock checks and obtain native Mac validation before claiming real TCC/signing behavior. See [validation](docs/validation.md) and [release process](docs/releasing.md).

Do not reset permissions, rotate keys, change bundle identity or alter credential storage incidentally while fixing unrelated behavior. Keep release tags immutable and publish license files, corresponding source, runtime notices and checksums alongside artifacts. Repository privacy remains an owner-controlled decision.

Contributions to the project's original code and documentation must be available under [GPL-3.0-or-later](LICENSE). Only submit work you have the right to contribute; preserve third-party license and copyright notices and identify any imported code and its license.

中文：提交应小而明确，采用 feat/fix/docs/test/chore 等前缀；英文文档变更同步中文。不要提交构建产物、日志、私钥、密钥或个人连接信息。忽略规则不会自动移除已跟踪文件。传输/输入改动运行协议和客户端测试；Mac 脚本模拟测试不能代替真实权限验证。不要为无关修复重置权限或改变凭据身份，发布 tag 保持不可变。原创代码和文档贡献须可按 GPL-3.0-or-later 提供；仅提交有权贡献的内容，保留第三方版权与许可说明，注明引入代码及其许可证。
