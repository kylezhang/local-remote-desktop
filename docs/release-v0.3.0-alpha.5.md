# Local Remote Desktop v0.3.0-alpha.5

**Windows x64 / Apple Silicon Mac correction preview.** Physical alpha.4 problems
were reported: Mac picture without usable input, and Windows unable to start
accepting. This version improves operation-specific permission handling, display
query compatibility and diagnosis. Those exact physical failures have not yet
been independently reproduced or certified resolved. Interactive tests remain
paused; native CI/package checks are required before publication.

## Changes

- Mac checks CGPreflightPostEventAccess for CGEvent sending and records AX separately.
  The explicit permission action calls CGRequestPostEventAccess; passive refresh
  never prompts, changes credentials or starts sharing. Returning from Settings
  refreshes the UI; Locate current app helps add the correct installed copy.
- Mac sends optional inputEnabled/eventPosting fields. Windows prefers the actual
  input-operation permission and retains older AX-only peer compatibility.
- Windows falls back to monitor rectangles under physical DPI awareness when
  current display-mode fields are unavailable. Capture/input bounds stay aligned.
- Windows startup now distinguishes occupied ports, denied listening, missing
  displays and other errors, shows sanitized native codes and offers Copy sharing
  diagnostics. FFmpeg is not wrongly blamed before any encoder was started.
- Startup failure/retry, permission precedence/denial and both desktop-role
  background regressions were added or retained.

## Install and remaining checks

Both repositories receive identical Windows ZIP, Mac ZIP/DMG, exact GPL-3.0-or-later
source, provenance and SHA-256 files. Older releases are retained.

Windows: quit your old copy before replacing files, extract the complete ZIP and
run LocalRemoteDesktop.exe. .NET is included; FFmpeg remains external and must
support H.264. Expand Troubleshooting if accepting fails; copy its new diagnostic
rather than guessing at FFmpeg or changing ports/security settings.

Mac: quit the old app, drag the DMG app into Applications, then launch the installed
copy. Ad-hoc/unnotarized updates may require normal first-open or renewed control/
Keychain approval. Use Allow keyboard and mouse control if needed, then return
from Settings; missing rows can be added using Locate current app. Do not disable
Gatekeeper, reset privacy databases or repeatedly reinstall. macOS 14+ still uses
the main-display picker; capture authorization is independent from input.

Input counters/permission APIs do not prove another app accepted events. Requested
owner diagnostics and actual Windows native failure are still needed. Real TCC/
upgrade, RDP/virtual-display behavior, two-device clipboard/input and sustained
hardware performance remain pending. One main display/controller, private IPv4,
logged-in awake sessions; no pre-login/FileVault/secure-desktop bypass. macOS
14–15.1 picker requires a single-display setup. No folder/raw-image clipboard.

## 中文

**Windows x64 / Apple Silicon Mac 修正预览。** alpha.4 被报告 Mac 有画面不能输入、
Windows 无法开始接受。本版改善实际输入权限判断、显示器查询兼容和具体诊断，
这两项实机问题尚未独立复现/确认修复；交互测试继续暂停，须原生 CI/包检查后发布。

Mac 改用 CGEvent 发送权限作为实际输入判断，AX 分开记录；仅主动允许时申请，
返回设置后刷新，未列出安装副本时可定位并手动添加。Windows 优先新 inputEnabled 字段，
兼容旧 AX 字段。显示模式不完整时启用物理 DPI 的监视器备用查询；启动错误区分端口占用、
系统拒绝、显示器不可用及其他原因，并提供可复制去敏错误码，不再误指尚未运行的 FFmpeg。

两站使用相同 Windows ZIP、Mac ZIP/DMG、精确 GPL 源码、来源和校验，旧发行保留。
Windows 退出旧程序后解压运行，.NET 已包含，FFmpeg 仍需外部完整安装；启动失败时展开
故障排查，复制共享诊断。Mac 退出旧程序后从 DMG 拖入 Applications，再打开安装副本。
临时签名/未公证可能需首次打开、控制/钥匙串确认；不关闭 Gatekeeper、重置隐私库或反复重装。
macOS 14+ 屏幕选择与输入权限仍独立。

输入计数/权限 API 不等于目标应用实际接受，仍需用户输入诊断和 Windows 实际启动错误。
真实授权/升级、RDP/虚拟显示、双机剪贴板/输入及持续性能待验收。仅主屏幕/一个控制端、
私有 IPv4 可达、正常登录唤醒会话；不支持登录前/FileVault/安全桌面绕过。
macOS 14–15.1 需单显示器，不支持文件夹/原始图片剪贴板。
