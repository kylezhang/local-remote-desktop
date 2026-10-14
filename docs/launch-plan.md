# Public launch plan

[简体中文](zh-CN/launch-plan.md) · [Desktop readiness](v0.3.0-plan.md)

Prepared **2026-10-14**. This is a proposal and asset checklist, not permission
to post, message communities, purchase promotion or claim a stable release.

## Positioning

**Control your Windows PC or Mac on the local network.**

Lead with the original mixed Windows/Mac workflow: direct current-session control,
one app per desktop, remembered pairing, text/files and no account/cloud relay.
AI-assisted development belongs in the maker story, not the main product benefit.
Use open-source/GPL-3.0-or-later accurately; avoid paid-tool comparisons or promises
of perfect latency, audited security or permanently prompt-free Mac upgrades.

Current public status is **v0.3.0-alpha.3**. Native builds and automated publication
passed; complete installed permissions, upgrades, clipboard and sustained-use
acceptance remains pending. Future mobile/Linux/HarmonyOS roles are roadmap only.

## Repository front door

The GitHub API audit found description/homepage empty, no topics, Issues enabled
and Discussions disabled. Source mirroring does not establish these settings.

Proposed GitHub About fields:

- Description: **Open-source remote desktop for Windows and macOS over a local network. Direct connections, no accounts or cloud relay. GPL-3.0-or-later.**
- Website, until a real site exists: **https://github.com/kylezhang/local-remote-desktop/releases**
- Topics: **remote-desktop, remote-control, lan, windows, macos, peer-to-peer, swift, wpf, dotnet, clipboard, file-transfer**.

These fields are prepared, **not yet applied**. The current connector has no
repository-settings write tool; browser binding timed out. No access token is
requested or copied, and no source-mirror configuration is changed.

Bug/feature forms are added under .github/ISSUE_TEMPLATE; they appear when the
existing mirror receives this commit. Keep one GitHub feedback queue initially;
do not open multiple forums before there is capacity to maintain them. Confirm a
private vulnerability-reporting channel before wider promotion; do not direct
security details to a public bug form.

## Promotion kit before outreach

| Asset | Purpose | Ready when |
|---|---|---|
| English/Chinese repository overview | Explain value, support and download choice | Current packages, prerequisites and alpha limitations are aligned. |
| Social preview, 1280 × 640 PNG under 1 MB | Recognizable repository links | Approved existing logo, one benefit line and accurate Windows/Mac support; no invented website/platform claims. |
| Three genuine screenshots | Show role entry, accepting and active desktop | Dedicated sanitized session, actual app/version; design mockups are labeled and never presented as runtime proof. |
| 30–60 second demo | Show a complete daily workflow | Connect, type/click, clipboard/file paste, disconnect; both directions only when recorded and validated. |
| Setup and FAQ | Reduce installation friction | Explain external FFmpeg, Mac first-open/picker/Accessibility and existing-VPN/manual-IP behavior without asking users to disable protections. |
| Channel-specific drafts | Make each post useful | English/Chinese maker story, usable downloads, known limits and a focused feedback request; owner review before publication. |

Do not open/record the owner's desktops to create this kit while acceptance is
paused. Real screenshots/demo wait for an agreed period. Installation friction,
especially external FFmpeg and Mac approval, is a priority before broad exposure;
dependency bundling needs a separate packaging/license decision.

## Rollout

1. **Repository preparation now:** About fields when admin access is available,
   feedback forms, download/FAQ clarity and planned visual assets. Keep coding
   fixes and native CI moving; no app upgrade or new release implied by doc work.
2. **Small preview invitation:** aim for 10–20 volunteer mixed Windows/Mac users,
   with explicit alpha status and current prerequisites. Start with a few relevant
   Chinese/international developer communities; collect reproducible first-use
   feedback before increasing reach. Invitations/posts need owner authorization.
3. **Desktop beta campaign:** after P0/basic two-direction acceptance, publish a
   genuine demo and tailored posts. Consider V2EX, technical articles/video in
   Chinese; Show HN and an English build story internationally. Check each
   community's current self-promotion rules before posting; no automated mass-posts
   or requests for votes/stars. Show HN requires something users can actually try.
4. **Broader desktop launch:** after the full desktop gate, consider Product Hunt,
   relevant remote-work/open-source communities and software directories. Prepare
   a clear product/download page and capacity to answer installation issues first.
   No paid promotion is proposed for the initial rollout.

Use GitHub as the international code/download entry and AtomGit as the primary
source/domestic download entry. A later static landing page can route to each;
it is a project site, not a desktop relay/server. Do not deploy a site merely
because it is listed here.

## Outcomes and follow-up

Track aggregate repository traffic where available, release downloads, voluntary
reports of first successful connection and repeated use, reproducible defects and
resolution time. Stars are secondary; do not add application telemetry as part
of promotion. Requests for security/unsupported platforms are handled honestly.

Next: apply accessible metadata, prepare the preview card/copy and tighten setup
instructions. Then present one complete promotion kit for review. Real demos and
physical acceptance remain paused until explicitly resumed.

References: [GitHub topics](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/classifying-your-repository-with-topics),
[social preview](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/customizing-your-repositorys-social-media-preview),
[Show HN guidelines](https://news.ycombinator.com/showhn.html),
[Product Hunt launch preparation](https://www.producthunt.com/launch/preparing-for-launch).
