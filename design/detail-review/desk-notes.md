# Screen desk: a different visual composition

[简体中文](desk-notes.zh-CN.md) · [Interactive proposal](desk.html?lang=en) · [Comparison](comparison.png) · [Three screens](desk-overview.png)

2026-10-16. **Accepted composition; surface refinement under review, no native changes.**
See the [latest browser review](refinement-2026-10-16.md). The owner rejected
the previous detail-only study because its overall appearance barely changed.
That study remains in `index.html`; it is not the proposed replacement.

The [native implementation checklist](../../docs/screen-desk-implementation.md)
records source mapping and staged validation as of 2026-10-17. It does not mark
the surface refinement or native migration as completed.

## Direction before implementation

Preserve the product job, two role names, pairing and stop rules. Reconsider the
visual organization instead of treating the old layout as immutable.

Palette: brand `#5265EC`, ink `#252D40`, secondary `#637084`, device stage
`#F4F6FF`, separator `#E3E7EF`, success `#28745E`. Fonts remain native system
families; the actual glyph mapping is an implementation requirement. Regular and
medium weights, 27/26 page, 19 device, 17 role, 13 control, 11–12 secondary sizes.
The 600 × 680 frame and smaller 420 × 568 preview retain independent scrolling.

```
Home:       centered question / one continuous display scene / two joined actions
Connect:    heading / one Add target / prominent remembered device / other devices
Accept:     heading / state / local-device identity / IP and password / bottom action
Settings:   heading / compact General, Connection, Clipboard and About groups
```

One illustration represents the connection relationship; the role actions sit
below it rather than each repeating an independent diagram. This reduces the
box-heavy impression and gives the product a coherent central visual. The devices
have clean hardware outlines and a muted desktop abstraction. These are generic
device illustrations, never live previews, screenshots or discovery metadata.

The device gallery makes the fictional remembered device prominent and keeps
other targets in a compact grid. A paired-device thumbnail is not authorized
screen content. A real implementation should feature a remembered available
device by a documented rule, and remain a regular grid if there is none; it must
not invent history, OS types or device names from a manually entered IP.

The accepting screen is organized around this device's identity, then its address
and password. The Stop action remains at the bottom and system permissions remain
separate. Settings shows useful preferences together, instead of devoting most of
a tabbed page to one language option. Group boundaries and native font conventions
remain shared across platforms.

## References and actual evidence

The [Design Spells](https://designspells.com/) catalog provided directions:

- [Linear reconnection feedback](https://designspells.com/spells/reconnecting-animation-when-offline-in-linear):
  state feedback belongs near the action. Our deliberate Connect has a short busy
  indicator; this proposal does not add a real reconnection implementation.
- [Are.na fluid sheets](https://designspells.com/spells/fluid-sheet-interactions-in-are-na):
  our own pairing/edit sheet enters from the lower edge while retaining device context.
- [Raycast page controls](https://designspells.com/spells/page-controls-in-raycast-wrapped-2023):
  a reason to evaluate navigation and composition together, rather than decorating
  the existing boxes. The resulting composition is our own interpretation.

**External browser pages timed out. Only the catalog/article titles and
categories were retrievable; their videos were not visually reviewed.** Do not
claim their full UIs, motion timing or source code were inspected or copied.
No third-party artwork was imported. The new SVG illustrations are original.

Review against the brief removed new cloud/account UI, explanatory Home subtitles,
repeated branding, permanent credentials and continuous decorative animation.
The illustration is the main visual change. Other screens use plain controls and
structured device information, not a new dashboard. Transitions are user-triggered
and respect reduced motion. The old offline fixture supplies validation, role
conflicts and mock states; none touches the real OS.

## Exports and checks

`export-study.cjs` creates static vector designs and PNG boards using sharp.
They are design exports, **not browser or installed-app screenshots**. The left
side of `comparison.png` uses the historical approved browser render; the right
side is the new static composition. Boards use Chinese for this review; the
interactive proposal includes English/Chinese and both desktop chrome styles.

The initial static boards were opened and visually inspected. JavaScript syntax, bilingual
keys, assets and isolated DOM-fixture states were checked; the loopback page was
verified independently. None certifies actual browser layout/keyboard behavior,
native typography, physical permissions or two-device desktop control. A static
export and CSS layout can differ; native implementation requires measured layout.

Local browser access subsequently became available; see the
[actual browser views and measured layout checks](refinement-2026-10-16.md).
The older static exports remain historical design references.

Next: review the surface refinement before implementation. Then adapt actual browser
layouts and adapt approved details to native Windows/macOS controls without
changing capture, transport, credentials or permissions as part of visual work.
