# Maintaining LocalRemote

Use small, focused commits with Conventional Commit-style subjects: `feat:`, `fix:`, `docs:`, `test:`, `chore:`. Describe user-visible behavior and validation; do not claim platform testing that was not performed. Keep README/roadmap statements aligned with implemented support. English is the primary documentation; update corresponding Chinese pages when changing behavior.

Do not commit build output, runtime binaries, release archives, credentials, pairing keys, logs, private certificates or user connection data. Check both `.gitignore` and already-tracked files: ignore rules do not untrack existing files. Avoid broad patterns that hide required icons, test fixtures or source assets. `.gitattributes` keeps shell scripts and source files on LF across platforms.

For changes to transport/input, run ProtocolChecks and ClientChecks on Windows with FFmpeg. For installer/signing changes, run isolated mock checks and obtain native Mac validation before claiming real TCC/signing behavior. See [validation](docs/validation.md) and [release process](docs/releasing.md).

Do not reset permissions, rotate keys, change bundle identity or alter credential storage incidentally while fixing unrelated behavior. Keep release tags immutable and publish runtime notices/checksums alongside artifacts. No open-source license has been chosen; repository privacy and licensing are owner-controlled decisions, not changed by this maintenance workflow.

中文：提交应小而明确，采用 feat/fix/docs/test/chore 等前缀；英文文档变更同步中文。不要提交构建产物、日志、私钥、密钥或个人连接信息。忽略规则不会自动移除已跟踪文件。传输/输入改动运行协议和客户端测试；Mac 脚本模拟测试不能代替真实权限验证。不要为无关修复重置权限或改变凭据身份，发布 tag 保持不可变。
