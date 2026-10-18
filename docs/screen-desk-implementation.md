# Screen-desk native implementation plan

[简体中文](zh-CN/screen-desk-implementation.md) · [Native progress](native-ui.md) · [Design preview](../design/detail-review/desk.html?lang=en)

2026-10-17. This is the implementation checklist for the reviewed screen-desk
direction. **Home implemented; both native CI jobs passed for source c23195b.**
The preview and its browser measurements are design evidence, not installed-app
acceptance. Earlier native progress entries remain historical.

The second batch passed both native CI jobs for source 20f4ecd; the Mac
English/Chinese task-page renders have been reviewed. See the
[task-page evidence](../tests/results/screen-desk-tasks-2026-10-17.md).
Batch 3 passed both native CI jobs for source 62a1d5b. The Mac session renders
were reviewed and an isolated, checksummed internal candidate was collected from
that run. See [session evidence](../tests/results/screen-desk-session-2026-10-18.md).
Installed two-device acceptance remains pending; no running client is replaced.

## Outcome and scope

One native app per desktop, with Start a connection / Accept connections as equal
roles. Keep the current logged-in desktop, remembered pairing, familiar input and
text/regular-file clipboard in one predictable workflow. The new composition
should improve navigation and visual hierarchy without replacing the transport,
capture, input or credential services.

Use the original screen-pair artwork once on Home, a device gallery on Connect,
centered local identity with vertically grouped IP/password on Accept, compact
grouped preferences, and a screen-first session bar. No notes feature, fabricated
desktop application, account flow or extra authentication step is introduced.
The neutral desktop placeholder is for the design preview; production shows real
decoded frames or a truthful connecting/empty state, never a fake connected screen.

## Source mapping and implementation gaps

| Area | Windows source | macOS source | Work required |
|---|---|---|---|
| Home | `apps/windows/MainWindow.xaml`, `BrandResources.xaml` | `apps/macos/Sources/LocalRemoteDesktop/App.swift`: DesktopShell, EntryTileStyle, AppBrand | Replace two independent illustration cards with one shared composition and two equal actions. Keep one Settings shortcut and active-role return routes. |
| Devices and pairing | MainWindow device template, RebuildDeviceMenu, DeviceConnectClick, ManualDialog | App.swift: ConnectionPage, ManualConnectionView; ControllerDiscovery.swift, ControllerMemory.swift | Gallery with one Add target action; open pairing only after explicit activation. Preserve draft, remember/forget, quality and error behavior. |
| Accept | `apps/windows/Hosting/SharingWindow.xaml` and its code-behind | App.swift: HostView, SharingActionView; HostModel.swift | Local identity, addresses and password in one vertical flow; reachable fixed Start/Stop. Reuse existing editors and lifecycle guards. |
| Permission/error guidance | Hosting/SharingFailure.cs, SharingWindow diagnostics | ScreenSharingAccess.swift, MacInputAccess.swift, HostModel.swift | Bind truthful capability/status to focused recovery actions. Preserve native screen selection and independent input approval. |
| Preferences | MainWindow SettingsPage and SaveDefaults | App.swift: DesktopSettings; SharingPreferences.swift | Replace sparse tabs with compact General / Connection / Clipboard / About sections; preserve each setting's scope and native controls. |
| Session | MainWindow SessionPage, SetSessionLayout, HomeBackClick, ToggleFull | DesktopShell; ControllerView.swift: ControllerView, RemoteScreenView | Compact device/status bar, options, native fullscreen, visible Disconnect, interrupted-connection overlay and temporary transfer notice. |
| Shared resources | UiTypography.cs, assets/localization/strings.json | UiText.swift, AppBrand, build-app.sh | Reuse native font rules and bilingual catalog; package original artwork at crisp sizes without a new runtime renderer. |

The session navigation gap is behavioral, not cosmetic. Windows currently blocks
HomeBackClick/NavigateClick while a session exists; Mac DesktopShell always shows
ControllerView while the controller is busy. Both need a separate current-page
selection, with session ownership retained independently. Navigating Home or
Settings must not disconnect, restart authentication or read credentials again.
Home offers Return to session; incompatible role actions explain the conflict.
Leaving the remote canvas releases held input and removes its keyboard ownership.
A connected session remains distinguishable from a listening local device.

Device artwork is generic. Names/OS labels come from advertisements or authenticated
peer metadata, not the entered IP. Discovery does not prove trust. “Remembered”
must correspond to actual stored connection metadata; cached advertisements and a
nonempty password field are insufficient evidence. Feature at most one recently
successful remembered peer when that history is available; otherwise use the
normal gallery. Unavailable records remain editable/manual-connect candidates,
not falsely online or silently deleted. Do not persist new history in this visual
pass without an explicit storage design.

## Visual implementation contract

- Accent #5265EC, ink #202D46, secondary #617088, rule #DFE5EF and soft device
  surface #F0F4FF. Status always includes text, not color alone. Keep brand,
  launcher, stable bundle/Keychain identifiers and installed credentials intact.
- Default outer frame remains 600 × 680 logical units, constrained to work area.
  Preserve user resizing across navigation. The first native batch retains the
  existing supported minimum; 420 × 568 is a preview stress case, not a verified
  new native minimum.
- Use existing Windows composite Segoe UI / Microsoft YaHei UI mapping and
  supported Regular weights. Mac uses system typography and native weights.
  Match hierarchy, alignment and density rather than assuming CSS font sizes or
  intermediate weights produce identical native glyphs.
- One central illustration is the Home focal point. Use vector geometry where
  supported or deterministically exported scale variants from the original SVG.
  Keep decorative effects on separate layers; text must remain sharp.
- Use native window chrome and native form behavior. The web bottom sheet is a
  task-panel reference: Windows uses an owned in-window panel; Mac an attached
  native sheet. Cancel/Escape retain or discard the correct draft and return focus.
- Routine content fits the default frame in both languages. Long lists, large
  text, errors and small work areas retain real internal scrolling; action areas
  remain reachable. Do not clip overflow or shrink text to make checks pass.
- Hover/press/focus should be distinct and restrained. Respect reduced motion,
  contrast/high-contrast settings, accessibility names and keyboard order.
  Do not replace platform switches with bitmap pictures.

## Ordered delivery batches

| Batch | Deliverable | Gate before continuing |
|---|---|---|
| 1 — Foundation and Home | Shared artwork exports/tokens; native Home in both apps, resource packaging, clear role choices | Native build and unshown bilingual layout/font/resource checks. Native lifecycle behavior unchanged. |
| 2 — Task pages | Device gallery and pairing panel; Accept layout and editors; compact preferences in both apps | Draft/error/cancel and role-conflict regression checks; default page fit, long-content scrolling, fixed Stop. |
| 3 — Session | Navigation independent of session; return-to-session route, compact options, reconnect/view-only feedback, transfer notice | Recording-service checks prove no reconnect/credential read on navigation, held input released, options constrained by global consent and disconnect cleanup intact. |
| 4 — Candidate | Review native renders against the browser references; exact-source CI packages, evidence and known limits | Both native CI jobs and package checks pass; no replacement of a running client. Then prepare one consolidated owner acceptance session. |

Each batch includes both desktops. Do not ship a Windows-only visual update as
proof of cross-platform completion. Existing concrete input and listener failures
remain P0: improving their presentation does not resolve them. Any required service
fix is a focused change with independent regression evidence.

## Verification and release checklist

- [x] Inspect current native UI entry points and identify lifecycle differences.
- [x] Record shared hierarchy, platform adaptation and the implementation order.
- [x] Implement shared Home composition and package its original artwork on both desktops.
- [x] Complete batch 1 native CI build/layout/package gates for source c23195b.
- [x] Implement batch 2 device/pairing/Accept/grouped-settings sources.
- [x] Complete batch 2 native CI and Mac render review for source 20f4ecd.
- [x] Implement session batch 3 sources: independent navigation, return route,
  compact native toolbar and truthful reconnect/view-only/transfer feedback.
- [x] Complete batch 3 final-source native CI and session render review for 62a1d5b.
- [x] Extend existing ClientChecks (Shell/Pairing/Sharing/Recovery/SessionInteraction/
  Typography) and MacLayoutChecks for the changed behavior and layout. Use
  recording services, isolated stores and generated frames; retain the existing
  protocol and input regressions. Update test assumptions for grouped settings.
- [ ] Review actual native renders in English/Chinese at default and supported
  minimum sizes, plus long names, addresses, errors and scaled text.
- [x] Run the existing GitHub Actions native builds/checks through the AtomGit
  mirror. Record the exact source commit, package identities and reports.
- [ ] During an agreed session only: verify active native colors/focus/sheets,
  system permission flow and real two-device input, clipboard, reconnect and Stop.
  No unsolicited SSH, foreground windows, personal clipboard reads or OS input.
- [ ] Select the next unused alpha version only when a package candidate is ready.
  Do not bump the version for this plan. Beta/stable gates remain in the delivery
  plan; a visual refresh or CI success cannot clear unresolved P0 failures.

The planning pass changed no native code. The subsequent Home batch changes
presentation/resources and the interface revision (Mac internal build 31), while
retaining public version, bundle identity, credentials and role lifecycles.
Windows local compilation, native glyph checks and full background regression
passed. Both native CI jobs passed; the cloud Mac Home render was inspected.
See [the evidence record](../tests/results/screen-desk-home-2026-10-17.md).
No installed app, permission or release is changed by this source update.
