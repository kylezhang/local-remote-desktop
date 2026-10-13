# Native Mac layout checks

```sh
LOCALREMOTE_LAYOUT_OUTPUT=/tmp/lrd-layout-renders bash tests/MacLayoutChecks/check.sh
```

Requires a logged-in macOS graphical session and the Apple SDK. The dedicated
test executable has a separate bundle identifier and a prohibited activation
policy. Its windows are never ordered front, made key or shown. It renders its
own views into bitmaps; it does not capture the user's screen.

Production SwiftUI/AppKit views and models are compiled with recording controller
services. The disposable App.swift copy excludes the application entry point,
uses a documentation IP in the displayed address list and seeds the selected
settings category. No production action is clicked. Host sharing, connections,
Keychain access and pasteboard operations are not started; unexpected controller
credential/clipboard/file access fails the check. Preferences use an isolated
suite and a volatile language override.

Checks cover English/Chinese Home, empty device list, Accept, all four settings
categories and ordinary target/password dialogs. Default shell content measures
600 × 658 logical units, inside the 600 × 680 native frame. Inspected NSScrollView
containers must fit their content; saved renders need visual review too. Dialog
renders use bounded fixture sizes, not actual sheet sizing/interaction acceptance.

These are inactive, unshown window renders: native buttons may appear gray.
They do not establish active-window colors, keyboard focus, permission approval,
installed update behavior, real peer discovery or remote input acceptance.
Never add window activation or real input to make this routine check pass.

中文：只渲染测试自身不显示的原生界面，使用虚构 IP、隔离偏好和记录服务。
覆盖中英文常规页面与弹窗；未激活窗口按钮可能呈灰色，不代表实际激活后的主题。
不能代替安装、授权、焦点、真实发现与双机操作验收。
