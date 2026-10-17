# Native interface artwork

The screen-desk Home uses `connection-stage.svg` as its editable master and
`connection-stage.png` (1500 × 798, transparent, three pixels per logical unit
at its maximum 500-unit width) in both WPF and SwiftUI. The same original
composition is reviewed in [the proposal](../../design/detail-review/desk.html).
Package one shared stage, not a duplicate illustration in each role button.

Regenerate with `node scripts/render-entry-art.mjs` and a Sharp installation
(`NODE_PATH` can point at an existing one). The illustrations are task cues, not
replacement app icons: blue identifies the operated screen, and the curved arrow
shows connection initiation. No actual desktop data or OS logos are included.

The earlier 960 × 744 role illustrations and their
[SVG sources](../../design/entry-review/README.md) remain historical references.
Build 31 introduced the shared stage in place of the old per-role artwork.

Task pages additionally use `device-display.svg` and its 540 × 360 transparent
PNG export. This original generic display is not a peer screen thumbnail.
Build 32+ bundles include it for the device gallery and local-device identity;
older package resource inventories remain valid for their original build numbers.
