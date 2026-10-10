# UI review prototype

Open `index.html` in a browser. No install, server, account or external dependency
is needed. This is a review artifact, not application code.

- Choose a platform, language and light/dark theme in the outer preview controls.
- Explore Connect, Share this device, Remote desktop and Settings.
- Use Preview state buttons to inspect pairing, missing permissions, transfer,
  reconnect and empty-device scenarios.
- Studio Mac demonstrates a remembered connection. Office PC demonstrates first
  pairing; use the fictional password `Blueberry482`. Any other value shows an error.
- Try manual Target IP, password generation/save/cancel, disconnect and mobile
  controls. Native system/clipboard actions only show a simulation notice.

All state is ephemeral. No credential storage, capture, input injection, network
connection or actual transfer occurs. Linux and mobile selectors are future
platform design references, not implemented support. Mobile Share illustrates
view-only screen sharing; it does not claim remote input or current support. The prototype uses the
approved repository icon rather than introducing a new brand.

See [English specification](../../docs/design.md),
[中文设计规范](../../docs/zh-CN/design.md) and [delivery plan](../../docs/v0.3.0-plan.md).

## 中文

浏览器打开 `index.html` 即可。外部工具栏切换平台、语言和主题，下方预览状态可查看
首次配对、权限不足、重连、传输等流程。首次配对的示例密码是 `Blueberry482`。
全部交互为模拟，刷新清空状态；不会访问系统或保存凭据。设计确认后才应用到原生客户端。
