# Online builds

[简体中文](zh-CN/continuous-integration.md) · [Development handbook](development-handbook.md)

GitHub Actions is the primary build and automated-test environment. Local builds
are optional for development. AtomGit remains the source repository; the existing
mirror sends commits to [GitHub](https://github.com/kylezhang/local-remote-desktop).
The workflow checks out that mirrored commit directly and does not manage mirroring.

The [first verified online build](../tests/results/github-actions-2026-10-13.md)
passed all three jobs on 2026-10-13. Downloaded Windows/Mac packages were checked
against their artifact/package hashes and corresponding source commit.

## When builds run

`Desktop CI` runs when `main` or a `v*` tag reaches GitHub, on pull requests to
`main`, and through **Actions → Desktop CI → Run workflow**. Pull-request updates
can supersede older runs; main/tag runs finish so publication is not interrupted. It requests
read-only repository access, uses pinned action revisions, and requires no extra
credentials for development builds. Actions must be enabled on the mirror.

Windows uses `windows-2022`, .NET SDK 8.0.425 and a checksum-pinned FFmpeg 7.1.1
test dependency. Mac uses an Apple Silicon `macos-15` runner and its Apple SDK;
the Swift/Xcode versions are retained with the reports. Native caches are not
shared across checkout paths. Source/installer/package fixtures run on Ubuntu.

## Download a build

1. Open [Actions](https://github.com/kylezhang/local-remote-desktop/actions/workflows/ci.yml).
2. Select a successful run and confirm its source commit.
3. Download `windows-x64-<commit>` or `macos-arm64-preview-<commit>` under **Artifacts**.
4. Extract the artifact wrapper, then extract the application ZIP inside it to a
   short, new folder. New Windows ZIPs put the app directly at the ZIP root; avoid
   adding another version-named directory. Run `LocalRemoteDesktop.exe` with all
   extracted files alongside it. If extraction is incomplete, the bundled
   `CHECK-INSTALLATION.cmd` reports missing/damaged files or overlong paths.
   `SHA256SUMS.txt` and `ci.json` identify the package and checks; `SOURCE.txt` and
   `source.zip` inside the application package supply corresponding GPL source.

Artifacts are retained for 14 days. GitHub requires a signed-in account with
repository read access to download them. An expired build can be rebuilt from its
commit; immutable published release assets remain a separate distribution path.

The Windows package contains the .NET Desktop Runtime. **FFmpeg is currently an
external runtime dependency**, not included in the package: reuse an installed
H.264-capable build or supply a complete build on PATH/beside the app. The pinned
CI FFmpeg is only used for generated-video tests. Bundling FFmpeg requires a
separate complete source/notices plan for the chosen third-party binary.

The Mac package is a complete Apple Silicon `.app` with icons and language/art
resources. It is explicitly **ad-hoc signed and not notarized**. macOS may block
opening a downloaded preview, and updates can require renewed permissions or
Keychain access. See the included instructions; do not disable Gatekeeper.
A signed product release requires the maintained Developer ID and notarization
flow in [Mac signing](macos-signing.md). CI packaging never installs/opens the app,
resets TCC or reads the user's pairing keys.

## What a green run establishes

- Windows native compilation; independent protocol/authentication, isolated DPAPI,
  clipboard/files, host lifecycle, synthetic pointer geometry and unshown WPF flows.
  The final Windows ZIP is extracted into an isolated directory and checked
  against its complete file inventory. Its actual self-contained EXE runs an
  explicit unshown startup check, loading the runtime and home resources without
  credentials, clipboard, discovery or sharing. The client suite then uses the
  extracted assembly. Missing runtime/files, failed startup and stale reports fail
  the job; the pre-archive publish directory is not the tested deployment.
- Generated H.264 software encode → encrypted loopback → decoder at all qualities.
  Hosted runners do not establish Media Foundation hardware performance.
- Native Swift fixture checks, full Apple SDK compilation, English/Chinese unshown
  layout checks, sealed app resources and extracted preview signature verification.
- Source/versions/licenses, localization coverage and mocked installer/release checks.

Real screens, physical mouse/keyboard, TCC/Keychain consent, real clipboard/LAN
workflow, hardware performance and sustained sessions remain explicit **Not run**
acceptance items. Missing hosted display capability is reported separately from
synthetic coordinate coverage. A green build does not clear release acceptance.
See [the desktop checklist](desktop-acceptance.md).

## Development and releases

Make changes, commit/push to AtomGit, inspect the resulting GitHub run, and fix
failures from its reports. Local compilation is optional; Windows use/testing can
consume the verified Actions artifact after the current client exits. Do not
overwrite a running client or change personal permissions merely to deploy a build.

`scripts/ci/` contains reusable entry points. `LOCALREMOTE_CI_CHECKS=1` selects
software/generated Windows capture; `LOCALREMOTE_SKIP_NATIVE_BUILD=1` avoids
duplicate full Swift builds between fixtures because the Mac packaging step builds
the full app. Neither switch is a native acceptance pass.

An approved `.github/release-request.json` lets the publish job create the GitHub
pre-release; `scripts/sync-atomgit-release.py` then mirrors the exact verified
bytes to AtomGit. The mirror only adds missing attachments, stops on any
checksum conflict because published artifacts are immutable, and sets
`release_status` only after every attachment passes read-back verification.
All six files must match GitHub API digests, the checksum list must cover every
archive, and `release.json` must match the published version, tag commit and hashes.
Every existing AtomGit file is checked before any missing file is uploaded.
The request's `atomgitStatus` selects `pre/latest` independently of GitHub's
prerelease flag; alpha.3 uses `latest` for the homepage. AtomGit may then report
`prerelease=false`; the alpha title/notes and GitHub pre-release remain unchanged.
This does not establish stable or native acceptance. The sync skips unless the `ATOMGIT_RELEASE_TOKEN` secret
exists. Tags alone still generate candidates, not automatic Releases.
Preserve existing tags/releases. The signed Mac release script still requires
Developer ID, notarization and stable versions; it never silently falls back to
an ad-hoc preview. Future signed-release automation will use protected secrets
and promote packages only after the documented acceptance gates pass.

References: [runners](https://docs.github.com/en/actions/reference/runners/github-hosted-runners),
[artifact downloads](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/download-workflow-artifacts).
