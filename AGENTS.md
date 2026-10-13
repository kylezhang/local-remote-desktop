# Product and development guardrails

Read [product direction](docs/product.md), [delivery scope](docs/v0.3.0-plan.md)
and [interface conventions](docs/interface.md) before changing product behavior.
Read the [development handbook](docs/development-handbook.md) before starting any
new platform. Keep its English/Chinese agreements aligned with implementation.

- Local Remote Desktop is one native application per supported platform. Desktop
  Connect and Share are session roles, not separate user-facing products.
- Focus on LAN direct access to the current logged-in desktop. Do not add accounts,
  cloud infrastructure, public traversal or a transport rewrite without a concrete
  authorized need. Do not promise pre-login/FileVault or secure-desktop bypass.
- Keep the usable Windows → Mac path covered while completing the reverse role.
  Discovery is not authorization; clipboard/files belong to paired session roles.
- Make routine use simple: remembered secure pairing, clear sharing/stop actions,
  compact session controls, native shortcuts and predictable updates.
- The current approved UI is in `design/entry-review/` (2026-10-12).
  Keep its role-first hierarchy across native apps; archive older proposals. Future platform mockups are not support claims.
- Prefer background/headless checks. Do not open foreground test windows, take
  keyboard focus or inject real OS input during the user's work. Native input
  acceptance needs an agreed test period and a dedicated target.
- Record native builds, fixture checks and real two-device acceptance separately.
  Do not treat generated code, mocks or screenshots as runtime acceptance.
- GitHub Actions is the primary native build and automated-test environment;
  local compilation is optional. Submit source to AtomGit and let its existing
  mirror trigger GitHub CI. Use the successful run's exact commit/artifacts for
  deployment and release candidates; do not manage mirroring or equate CI with
  physical desktop, permission or hardware-performance acceptance.
- Keep English primary and Chinese documentation aligned. Retain the approved
  brand. Avoid incidental identity, Keychain or permission changes during UI work.
- Keep secrets, personal device data, local test logs and build outputs out of Git.
  Follow GPL-3.0-or-later notices and the documented immutable-release process.
- After each owner-facing response, state the next concrete step. Complete independent
  coding/checks before requesting native acceptance; consolidate owner participation.
- Defer verified Windows deployment while the client is in use. Never close/reopen
  it or show test windows to make a background check pass.

See [中文产品方向](docs/zh-CN/product.md) and [中文交付计划](docs/zh-CN/v0.3.0-plan.md).
