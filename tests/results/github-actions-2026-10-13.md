# First verified GitHub Actions build — 2026-10-13

Source: `d12c5702d60c4d17393f6dafb2fe286a476abc1e` (0.3.0-alpha.2).
[Desktop CI run 38360478631](https://github.com/kylezhang/local-remote-desktop/actions/runs/38360478631)
completed successfully on the existing GitHub mirror. No local native compilation
was required for this build. AtomGit-to-GitHub mirroring was not changed.

| Job | Result | Scope |
|---|---|---|
| Source and packaging checks | Pass | Workflow lint, source/version/license/localization checks and isolated installer/signing/release-package fixtures on Ubuntu. |
| Windows x64 | Pass | Native self-contained build, seven backend/logic suites, exact packaged assembly's unshown WPF/client checks; generated software H.264 at three qualities through encrypted loopback and decoder. |
| macOS Apple Silicon | Pass | Native Swift fixtures, English/Chinese unshown layout checks, full Apple SDK app compilation, ad-hoc bundle seal and extracted preview signature/provenance verification. |

Both application artifacts were downloaded independently and their complete
artifact ZIP SHA-256 values matched the GitHub API digests. Inner application
ZIPs matched SHA256SUMS.txt; source archives identify the exact commit and GPL
license. Windows runtime/client files and Mac resources/signing/preview manifest
were checked without launching either application.

| Application ZIP | SHA-256 |
|---|---|
| `LocalRemoteDesktop-v0.3.0-alpha.2-windows-x64.zip` | `66914ee7e250ca516a04ccf2ff64fba54f36b91fdfe9ffefc8d1da6385fd1fa3` |
| `LocalRemoteDesktop-v0.3.0-alpha.2-macos-arm64-preview.zip` | `4903591f5ec18ed8f079f4ad58863b4a6afad2d8190af7a347caefd4bcb529e3` |

Artifact downloads (GitHub sign-in/read access required; expire 2026-10-27):

- [Windows application](https://github.com/kylezhang/local-remote-desktop/actions/runs/38360478631/artifacts/11793894622)
- [Mac development preview](https://github.com/kylezhang/local-remote-desktop/actions/runs/38360478631/artifacts/11793804483)
- Reports are separate artifacts on the run page. App downloads contain an outer
  artifact ZIP and an inner application ZIP; extract both in sequence.

## Boundaries

Mac is ad-hoc signed, not Developer ID signed/notarized; renewed system approvals
may still occur. Windows .NET runtime is included, FFmpeg remains external.
Hardware performance, physical input, actual screens, local-network permissions,
Keychain/TCC, real clipboard and sustained sessions are not cleared by this run.
See [desktop acceptance](../../docs/desktop-acceptance.md). No new tag/release was created.

中文：首轮 GitHub 在线源码检查、Windows 原生编译/后台检查和 Mac 原生编译/布局/预览包检查均通过。
下载后再次核对了外层产物摘要、应用 ZIP、源码版本与包内资源；未启动应用。
产物在运行页面的 Artifacts 中，需登录下载，保存至 2026-10-27；先解压外层，再解压应用包。
Mac 是临时签名、未公证预览；Windows 仍需外部 FFmpeg。真实输入、系统权限、硬件性能、
实际剪贴板和长时间会话仍需单独验收，不因此发布新版本。
