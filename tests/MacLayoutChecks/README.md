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
uses documentation IPs in the displayed address list and fictional gallery peers.
Settings are now four continuous groups. No production action is clicked. Host sharing, connections,
Keychain access and pasteboard operations are not started; unexpected controller
credential/clipboard/file access fails the check. Preferences use an isolated
suite and a volatile language override.

A disposable ControllerView.swift copy appends a same-file extension to seed
the long pairing-error fixture. The production private setter is unchanged;
the extension is not compiled into the app or package.

Checks cover English/Chinese Home, empty device list, a three-device gallery,
Accept, grouped settings, ordinary/known-target pairing, long pairing errors and
password dialogs. Long-error fields may scroll; their saved renders must be
reviewed for the fixed footer rather than claiming no overflow. Default shell content measures
600 × 658 logical units, inside the 600 × 680 native frame. Inspected NSScrollView
containers must fit their content; saved renders need visual review too. Dialog
renders use bounded fixture sizes, not actual sheet sizing/interaction acceptance.

These are inactive, unshown window renders: native buttons may appear gray.
They do not establish active-window colors, keyboard focus, permission approval,
installed update behavior, real peer discovery or remote input acceptance.
Never add window activation or real input to make this routine check pass.

中文：只渲染测试自身不显示的原生界面，使用虚构 IP、隔离偏好和记录服务。
覆盖中英文首页、空列表、三设备展陈、接受连接、连续设置分组及弹窗；
未激活窗口按钮可能呈灰色，不代表实际激活后的主题。
不能代替安装、授权、焦点、真实发现与双机操作验收。
