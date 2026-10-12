# Task-first entry review

[简体中文](README.zh-CN.md) · [Design notes](../../docs/entry-design.md)

**Approved on 2026-10-12; current native implementation reference.**
First launch chooses Start a connection or Accept connections (接受连接); the next screen owns
its task. Open `index.html` offline, or serve the repository and open
`design/entry-review/index.html`. The outside page/platform/language controls are
review tools, not product navigation. The draft includes Windows/macOS chrome,
English/Chinese copy, an empty list and Mac permission state.

All devices/passwords are fictional, kept only in this preview's memory. The
fixture target password is `Blue482X`. Copy/refresh/permission/connect provide
simulated feedback: no clipboard, network, file, credential or system action.
Selecting a role never enables sharing. A sharing-to-connection conflict has an
explicit confirmation; Cancel preserves sharing. The browser screenshots are
mockups, not evidence of native runtime behavior.

[Overview](overview-zh.png) · [Home](windows-home-zh.png) ·
[Connect](windows-connect-zh.png) · [Accept connections](mac-share-zh.png) ·
[Empty state](mac-connect-empty-zh.png) · [Add target](mac-add-target-zh.png)

Headless browser review covered EN/ZH × Windows/macOS within the 560 × 568 draft
frame, no horizontal overflow at a narrower viewport, entry actions, pairing
feedback, password draft/cancel/save, permission state and role confirmation.
Native build/acceptance evidence is recorded separately in [native progress](../../docs/native-ui.md).

Current interactive/native default: 600 × 680. Stored PNGs above are earlier
560 × 568 browser captures; native overflow evidence is in the progress document.
