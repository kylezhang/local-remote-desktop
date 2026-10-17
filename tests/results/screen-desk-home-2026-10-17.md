# Screen-desk Home: first native batch

2026-10-17. Interface revision `screen-desk-2026-10-17-home`; Mac internal build
31. Public version remains 0.3.0-alpha.5. This is new source work, not a replacement
for the published alpha.5 assets.

## Changes

Both native Home pages use the same original 1500 × 798 transparent composition,
with two compact equal role actions below it. Package one shared stage instead of
the previous per-role images. Native window chrome, font conventions, role
navigation, active-sharing return, credentials and transport/input services remain.
Mac resource validation distinguishes build 31+ from older immutable packages.

The optional unshown Windows screenshot helper now composites rendered pixels
onto white, rather than using a live VisualBrush that remeasured the view.
Odd-pixel equal columns permit one pixel of rounding difference. A localized
preview query string is parsed separately from its documentation file path.

## Local evidence

- Windows .NET 8 build of production client and ClientChecks: passed, zero warnings.
  Output is isolated in a work directory, not an installed launch location.
- Complete default unshown ClientChecks: passed after the above harness corrections.
  Includes bilingual Home/default-frame fit, resource dimensions, real WPF glyph
  families/weights, manual pairing/drafts, active-listener navigation and editing,
  consent/default scopes, generated H.264, quality changes, retry/disconnect and
  recording-service input/text/file cleanup. No window was shown.
- English/Chinese Home renders saved from production WPF views and visually
  inspected. They are unshown native content renders without window chrome,
  not screenshots of the installed/active app.
- Swift 5.9.2 frontend parse of production sources in local Linux: passed.
  This is syntax only, not an Apple SDK build.
- Signing/build-shell fixtures: all 13 cases passed with mocked tools.
- Build-cache fixtures: all four methods passed on Linux.
- Mac package fixtures: all five methods, including 15 mocked release-pipeline
  cases, passed on Linux. New artwork, missing resources and older Home bundle
  compatibility are covered.
- Release/source checks: passed (versions, identity, docs, localization, icons,
  tracked-file hygiene and basic secret indicators).

Windows-hosted Bash attempts first encountered sandbox named-object restrictions;
after using the permitted fixture runner, cache path spelling/symlink privileges
and mock-package filesystem entries still differed from POSIX. These attempts
are not counted as passes. The same cache/package suites passed in local Linux;
production validation was not weakened to accommodate Windows filesystem quirks.
The installed tree-sitter Python binding did not match the bundled Python ABI;
Swift syntax was instead checked by the actual Swift frontend in Linux.

## Cloud and installed boundaries

Native Windows/macOS CI for the final source commit is pending at this record's
creation. The Mac SDK build, native Home render and signed-bundle resource check
must pass before calling this batch verified.

No real screen capture, OS input, personal clipboard, Keychain/TCC modification,
SSH connection, application installation, deferred deployment, tag or release was
performed. The earlier real input/listener failures remain unresolved acceptance
items; these appearance changes do not prove a fix.

中文：两端首页源码已更新，Windows 编译、完整后台回归及中文/英文原生内容渲染已检查。
Swift 仅完成真实编译器的语法分析；模拟签名、缓存和打包检查通过。
Apple SDK 构建及 Mac 原生渲染以最终提交对应的 CI 结果为准。
没有弹前台窗口、操作真实桌面或个人剪贴板、改权限、升级安装或发版。
