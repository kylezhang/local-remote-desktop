# Task-first entry review

[简体中文](README.zh-CN.md) · [Design notes](../../docs/entry-design.md)

**Draft for review. Native clients have not been changed for this proposal.**
First launch chooses Connect to a device or Allow connections (允许连接); the next screen owns
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
[Connect](windows-connect-zh.png) · [Allow connections](mac-share-zh.png) ·
[Empty state](mac-connect-empty-zh.png) · [Add target](mac-add-target-zh.png)

Headless browser review covered EN/ZH × Windows/macOS within the 560 × 568 draft
frame, no horizontal overflow at a narrower viewport, entry actions, pairing
feedback, password draft/cancel/save, permission state and role confirmation.
The owner still needs to approve this draft before native implementation.
