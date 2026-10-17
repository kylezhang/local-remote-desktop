# Interaction detail study

[简体中文](README.zh-CN.md) · [Open preview](index.html?lang=en) · [Approved design](../entry-review/index.html)

**2026-10-16 · Proposal for review.** This separate prototype does not supersede
the approved entry design, alter native clients or change a release. All devices,
credentials, permissions, screens and transfers are fictional and reset on reload.
Open `index.html` directly or serve the repository locally. No external dependency,
network request, clipboard access, credential storage or OS action is used.

## Design intention

Make a small remote desktop utility feel precise and predictable. Preserve the
two agreed roles, one device list, configure-before-start and an accessible Stop.
Spend visual emphasis on choosing the operated screen; keep forms and settings
quiet. Do not turn the utility into a dashboard or add decorative onboarding text.

| Token | Value and role |
|---|---|
| Accent | `#5265EC`, established brand and primary action |
| Ink | `#252D40`, main text |
| Secondary | `#637084`, explanations and labels |
| Surface | `#F5F7FA`, restrained grouping |
| Divider | `#E3E7EF`, real information boundaries |
| Success | `#28745E`, positive state with accompanying text |

Native typography remains Segoe UI with explicit Microsoft YaHei UI CJK mapping
on Windows, system San Francisco/PingFang on Mac. Browser fallbacks approximate
these fonts; they do not certify native glyph runs. Type scale: 26/25 page,
20 dialog, 19 role, 14 body, 12 label. Use a 4px spacing basis, 30px page insets,
600 × 680 desktop frame and a 420 × 568 stress view. Normal pages use one column.
Their scrolling region excludes the accepting action and modal buttons.

```
Home                    Connect                  Accept
          Settings      Back         Settings    Back         Settings
Main question           Title + description      Title + description
[ Start ] [ Accept ]    Devices   Refresh / Add   Status + local name
                        Device list              IP / password
                        → focused pairing sheet  Mac permissions, if relevant
                                                 ─────────────────────
                                                 Start / Stop
```

The homepage retains the approved display illustrations and icon. It adds no
subtitle. Illustration hover changes by 3px; the complete button remains the hit
target. No always-running animation, radar, confetti or decorative glow is added.

## References and adaptation

The owner supplied [Design Spells](https://designspells.com/). Its catalog surfaced
these references; they are inspiration for our own implementation, not assets to
copy or proof that their source implementation was inspected:

- [Sudoku a Day sheet transitions](https://designspells.com/spells/smooth-sheet-transitions-in-sudoku-a-day):
  use a restrained 200ms entrance for the focused pairing/edit sheet. Native clients
  should use native presentation, without rasterizing text or rebuilding transport.
- [Find My transitions](https://designspells.com/spells/animated-transitions-in-find-my):
  preserve the selected device's name/icon context into the pairing sheet and expose
  the real connection state. Discovery names are announcements, not authenticated
  identity. Manual IP entry has no invented device name or operating system.
- [Granola icon feedback](https://designspells.com/spells/sidebar-icon-animation-in-granola):
  keep interaction feedback local and brief. Here, hover/press, status labels and
  a temporary copied notice provide feedback without introducing a sidebar.

Review against the brief rejected a large gradient background, a new brand color,
three-dimensional page flips and a permanently expanding device panel. They would
add visual work without making connection faster. The memorable visual remains
the operated-screen illustration; ordinary controls stay native and legible.

## Flow and state details

- Role choice only navigates. Back does not stop accepting connections. A Home
  indicator returns to an active accepting role; an active remote session retains
  a return action when browsing other pages.
- One Add target action opens a focused sheet; activating a discovered device
  opens the same sheet with its announced name/platform/IP. Fictional remembered
  devices prefill the fixture password `Blue482X` only. No real secure store is read.
- A deliberate Connect simulates a 700ms busy state, then a clearly fictional
  remote-screen preview. Wrong IP/password retains the draft and inline error.
  An accepting role requires explicit Stop & connect after target validation.
- The receiving page stacks name, IP and masked password. IP is OS-provided, not
  editable. Generate changes an eight-character draft; Cancel does nothing; Save
  stops accepting and awaits a deliberate new Start. Existing legacy passwords
  remain an implementation compatibility requirement, not silently rotated by UI.
- Mac capture and keyboard/mouse control are separate rows. Missing capture blocks
  Start. Missing control keeps an explicit view-only warning, including while
  waiting. Allow/Locate only explain native actions; they never grant OS permission.
- The listener-failure fixture states the specific failure and offers diagnostics.
  TCP detail belongs in expanded troubleshooting, not a generic FFmpeg instruction.
  Native adapters must display the actual classified failure, never this fixed
  fictional port-conflict error for every failure.
- Settings retains General / Connection / Clipboard / About, with tabs and focused
  content rather than stacked unrelated settings. Preview toggles persist in memory
  across tabs; connection quality defaults and temporary session quality are distinct.
- The optional transfer fixture is a temporary notice with Cancel; no permanent
  transfer toolbar or second password prompt. Its percentage is a static fixture,
  not fabricated performance or a real file operation.

The session view sketches compact controls, not a complete remote-desktop redesign.
Simultaneous incoming/outgoing operation, new platforms and expanded protocol
capabilities are not introduced by this study. Full native conflict/lifetime rules
remain mandatory when implementing; a web mock cannot certify them.

## Interaction quality and review boundary

Visible keyboard focus, labeled controls, modal focus containment, Escape/Enter,
arrow-key settings tabs, English/Chinese strings, small-window scrolling and
`prefers-reduced-motion` are included. Controls use semantic buttons; status is
text plus color. Modal drafts survive validation and language redraws.

The browser automation connection was unavailable in this session. JavaScript
syntax, local HTTP availability, asset resolution, bilingual key parity and 16
DOM-stub render variants passed. Isolated state checks covered validation/draft
retention, manual identity, separate permissions, password save/cancel and both
role-conflict directions. These do not measure layout or physical interaction. Native
layout, actual browser screenshots, keyboard feel and OS fonts remain visual
acceptance work. Do not label this design as browser- or native-accepted.

Next: review the interactive proposal, then implement the approved details in
Windows/macOS incrementally. Keep input/permission correctness and incomplete
AtomGit publication as separate engineering work; aesthetic changes resolve neither.
