# Interface proposal

[简体中文](zh-CN/design.md) · [Product direction](product.md) · [Delivery plan](v0.3.0-plan.md)

**Reviewed on 2026-10-10; native implementation is underway.** The desktop
navigation and session layout are now in source. Windows passed an unshown WPF
layout/lifecycle check; the updated Mac UI awaits a native SDK build because the
development Mac is currently unreachable. Discovery and reverse clipboard/files
remain next-stage work. This prototype also includes future platform references. Open
[the interactive prototype](../design/review/index.html) in a browser. It runs
offline with fictional devices, passwords and desktop content. It never connects,
captures, transfers or stores credentials. Platform selectors include future
design references. Preview controls outside the app are not product controls.

## Layout decision

One app, two explicit desktop actions: **Connect** and **Share this device**.
Settings is a third navigation item. A narrow desktop navigation rail supports
device browsing; it disappears during remote control. Device rows show meaningful
identity, system, reachability and remembered pairing, rather than a dashboard
of cards. Manual Target IP is a secondary action when discovery is unavailable.

First pairing uses a small dialog. Returning connections reuse secure stored
credentials; discovery is never trust. The password stays masked on the sharing
page, with Reveal/Copy and an explicit edit dialog. Generation fills a draft;
Save applies it, Cancel changes nothing. Save explains that current connections
will end. Never automatically turn on sharing after connecting elsewhere.

Share is separate from connection state. Screen permission is needed to share a
Mac, Accessibility to accept input. Missing Accessibility permits clearly labeled
view-only sharing; connecting as a viewer does not require sharing permissions.
System dialogs stay native. Mobile designs retain a Share entry for future
screen-sharing capability. Sharing a picture and accepting remote input are
separate capabilities; the mobile proposal defaults to view-only and does not
promise full remote control. The actual app shows only implemented, validated
capabilities. See the [mobile feasibility note](mobile-sharing.md).

Routine screens do not repeat LAN-only badges or scope slogans. Explain network
requirements in setup/help and project documentation; reserve status indicators
for device reachability, sharing and the active connection.

## Remote control

Use one 40-pixel desktop session bar: target/status, quality, full screen, options
and Disconnect. No second logo, credential fields or permanent diagnostics panel.
Reconnect keeps the last picture visibly dimmed with an explicit reconnect state
and Cancel; never present stale pixels as a live connection. Transfers have a
temporary progress/cancel notice. The prototype's transfer and permission states
are scenarios, not evidence of implementation.

Mobile uses the same connection/pairing flow, bottom navigation before a session
and a compact bottom control dock during one. Direct touch and touchpad modes,
keyboard and session options replace desktop-only interaction. Safe areas and
44-pixel touch targets are required. Tablet layouts can use a list/detail split.
Linux/Android/iOS/HarmonyOS implementation follows their existing roadmap gates.

## Visual system

Retain the approved blueberry icon. Accent **#5265EC**, pressed **#4238AF**,
support **#643AC2**, brand ink **#282C67**, pale surface **#F7F8FF**. Gradient is
reserved for the existing brand asset. Use quiet white surfaces, fine separators,
modest selection tint and semantic status colors; avoid decorative glow.

Native system typography: Segoe UI on Windows, San Francisco on Apple systems,
platform default on Linux/mobile. Body 14–15px, secondary 12–13px, page heading
24px. Spacing follows 4/8px steps. Inputs/dialogs have modest corner radii;
top-level app windows retain native chrome. Desktop minimum design width 720px;
below that use the narrow-screen layout rather than clipping translated controls.

English is the primary catalog with Chinese equivalents. Copy names an action
and its result, with a useful next step for errors. Device names/passwords/files
are not translated. Keyboard focus is visible; dialogs trap focus and return it
on close. Respect Escape, screen-reader names, reduced motion and OS theme.

## Review and implementation gates

Review device selection, remembered connection, manual IP, pairing failure,
sharing permissions, password edit/cancel, a live-looking session, reconnect,
transfer and phone control. The visual review does not replace native QA.
After approval, extract shared vocabulary/tokens into the interface specification
and implement the two desktop UIs incrementally. Keep security, storage and
native permission behavior out of unrelated layout changes.
