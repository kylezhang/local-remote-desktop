# Release process

[简体中文](zh-CN/releasing.md)

1. Decide scope and version. Product versions begin at 0.1.0; subsequent changes use semantic major/minor/patch intent. Keep Windows csproj, window title, Mac plist/UI and changelog aligned. Mac CFBundleVersion is a monotonically increasing build number even though the public version resets once from development 0.2.x.
2. Update English and Chinese documentation. Retain compatibility identities unless a tested migration is intentional. Inspect tracked files, `.gitignore`, diff/whitespace and secret indicators; do not commit dist/bin/obj/cache, logs or certificates.
3. Use GitHub Actions as the primary Windows/macOS native build and automated-test environment; local compilation is optional. Record the exact successful run/source commit and distinguish CI fixtures, user feedback and real two-device acceptance. Never open test windows or inject input during the owner's work.
4. Package committed, clean source with `scripts/package-windows.ps1`. Include the project's GPL-3.0-or-later LICENSE, NOTICE.md, SOURCE.txt and source.zip, along with .NET runtime licenses/notices and the launch guide. source.zip contains the exact source commit, build scripts and documentation; recipients must not need access to the private repository to obtain corresponding source. FFmpeg is external. Generate SHA-256 checksums. Verify the ZIP contains the tested client DLL, icon, Desktop runtime and matching source. Do not publish unverified Mac binaries.
5. Push source to AtomGit and let its existing mirror trigger GitHub CI; do not change mirroring or push separately to GitHub. Use an annotated `vX.Y.Z` tag at the tested commit. Prepare draft GitHub and AtomGit Releases with the same cloud-built bytes, version-only title, bilingual notes, source and checksums. Verify both tags and downloaded assets before publishing. Retry only missing matching assets after partial upload failures. Tags and published artifacts are immutable; fixes require a new version.
6. The agreed local Windows executable paths remain deployment targets. Deploy the verified package when idle; while a client is in use, defer until it exits. Do not close/reopen it or spawn another instance during the owner’s work. Keep only one product launcher, retain saved connections and ensure any deferred job uses the newest verified candidate. Build output stays out of Git.

For a clearly labeled beta, a tested Windows ZIP plus Mac source may be published before native Mac acceptance. Set prerelease=true, disclose unverified Mac SDK/UI/Keychain and two-device behavior prominently, and attach no Mac binary. This exception does not authorize a stable release or satisfy native Mac binary acceptance. Candidate scope must be explicit; a beta does not imply every roadmap feature is complete.

The release notes must distinguish implemented features, automated test coverage, user-reported real-device behavior and unverified platforms. A 0.x release can be published as a normal release while explicitly documenting its early-product limits. Do not change repository privacy or select an open-source license implicitly.

On 2026-10-14 the maintainer explicitly deferred interactive tests and authorized
the complete compilation/publication flow. This permits the alpha.3 Mac binary
after successful native CI/package checks, with pending physical acceptance
prominently disclosed. `.github/release-request.json` identifies that specific
pre-release; `scripts/ci/publish-preview.sh` verifies the exact run's assets,
creates an annotated tag and a draft, downloads/verifies it, then publishes.
The job runs only on this repository's main branch, never pull requests, and
uses the Actions token without a maintainer PAT. It does not publish stable versions
or change source mirroring. AtomGit receives those same bytes through the
Actions API synchronization step; MCP remains available for inspection/recovery.

A Mac binary requires the [signing and distribution acceptance](macos-signing.md).

AtomGit distinguishes the last-updated release from its homepage **latest**
selection. After publishing, read back `release_status` through the MCP/API;
do not infer the homepage choice from list order. Use a short release name such
as `v0.2.0-beta.1`. On 2026-10-11, selecting the already published beta as `latest`
also made the API's `prerelease` field false; its version and notes still explicitly
identify it as a preview. This designation does not clear native acceptance gates.
Existing tags and download assets were unchanged. See the
[AtomGit update API](https://docs.atomgit.com/docs/apis/patch-api-v-5-repos-owner-repo-releases-tag/).

The CI mirror step `scripts/sync-atomgit-release.py` runs right after the GitHub
publish and enforces the same discipline: the token comes only from the
`ATOMGIT_RELEASE_TOKEN` secret; only missing attachments are uploaded; a
same-name checksum conflict halts the sync instead of overwriting; and
`release_status` is patched and read back only after every attachment matches
the verified GitHub bytes.

The API does not document an AtomGit draft parameter. Stage its release entry,
verify all existing assets before any upload, then select the requested status
after complete read-back verification. GitHub API digests cover all six assets;
the archive checksum inventory must be complete and unique, and provenance must
match the immutable tag. `atomgitStatus` in the release request (or
`--atomgit-status`) controls homepage selection separately from stability.
Alpha.3 selects `latest` without changing the GitHub prerelease or release notes.
The maintainer does not plan to purchase Apple Developer membership. An explicitly
ad-hoc signed, unnotarized package is a supported distribution path. Paid signing
does not determine feature stability or alpha/beta/stable status; native acceptance
does. Disclose first-open approval and possible Accessibility/Keychain reapproval
after ad-hoc updates. Do not disable Gatekeeper or promise unchanged permissions.
Shared Swift tests alone do not establish native permission acceptance.

Use `scripts/package-macos-preview.sh` for the current ad-hoc CI ZIP/DMG candidate.
Install via the DMG's Applications shortcut before first launch. On macOS 14+,
explicitly select the main display in the system picker; Accessibility remains
separate. See [screen access](mac-screen-access.md). CI artifacts are temporary
downloads; durable releases use the same verified bytes on both repositories.

For the optional Developer ID distribution path, after committing aligned versions, use `scripts/package-macos.sh`
with a Developer ID fingerprint and an existing Keychain notarytool profile as
described in [maintainer packaging](macos-signing.md#maintainer-packaging).
It checks the committed source, builds/signs/notarizes, validates the final extracted
artifact, and writes an immutable candidate folder with provenance and checksums.
Mac candidates also include LICENSE, NOTICE.md, SOURCE.txt and source.zip from that exact commit.
It does not install the Host or publish a release. Pass native first-launch/update
acceptance before tagging and uploading. This pipeline has only been mocked here.
