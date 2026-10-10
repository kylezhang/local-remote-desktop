# Product name decision

[简体中文](zh-CN/product-name.md) · [Interface conventions](interface.md)

Accepted by the project owner on 2026-10-10:

- Product display name: **Local Remote Desktop**.
- Repository slug for the next naming migration: **local-remote-desktop**.
- The name describes remote desktop access on a local network, including future
  phone/tablet controllers for supported desktop computers.

The current product and repository still use LocalRemote. This document records
the decision; the repository has not yet been renamed. Schedule a dedicated naming
migration during v0.3.0 development, with aligned application labels, documentation,
artifact names, build/install paths and AtomGit repository links.

Preserve paired credentials, remembered connections, the existing macOS permission
and Keychain identities, and released tags/assets. Human-facing names and storage
identities serve different purposes; changing the product name must not silently
reset permissions or passwords. Keep compatibility migration paths for current
installations and verify the user's existing Windows launch location.

Previously published LocalRemote versions remain historical releases. Record the
actual repository migration and installation behavior before describing the new
name as delivered. English remains the primary documentation language, with
Chinese equivalents maintained alongside it.
