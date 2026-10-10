# Release process

[简体中文](zh-CN/releasing.md)

1. Decide scope and version. Product versions begin at 0.1.0; subsequent changes use semantic major/minor/patch intent. Keep Windows csproj, window title, Mac plist/UI and changelog aligned. Mac CFBundleVersion is a monotonically increasing build number even though the public version resets once from development 0.2.x.
2. Update English and Chinese documentation. Retain compatibility identities unless a tested migration is intentional. Inspect tracked files, `.gitignore`, diff/whitespace and secret indicators; do not commit dist/bin/obj/cache, logs or certificates.
3. Build Release on Windows, run protocol/client and affected script tests, and record actual results in validation. Native Mac compilation and UI/signing testing requires a Mac; explicitly state when absent.
4. Package committed, clean source with `scripts/package-windows.ps1`. Include the project's GPL-3.0-or-later LICENSE, NOTICE.md, SOURCE.txt and source.zip, along with .NET runtime licenses/notices and the launch guide. source.zip contains the exact source commit, build scripts and documentation; recipients must not need access to the private repository to obtain corresponding source. FFmpeg is external. Generate SHA-256 checksums. Verify the ZIP contains the tested client DLL, icon, Desktop runtime and matching source. Do not publish unverified Mac binaries.
5. Commit source/documents and push main to AtomGit. Use an annotated `vX.Y.Z` tag pointing at the exact release commit. Create the AtomGit Release, upload ZIP/checksum assets, then verify tag commit, release body and downloadable assets. Tags and release artifacts are immutable: fix mistakes in a new patch release rather than retargeting a tag.
6. The existing local Windows executable path remains a deployment target. Close only that instance before replacing it, then reopen one instance. Do not copy build output into Git or remove saved connections.

For a clearly labeled beta, a tested Windows ZIP plus Mac source may be published before native Mac acceptance. Set prerelease=true, disclose unverified Mac SDK/UI/Keychain and two-device behavior prominently, and attach no Mac binary. This exception does not authorize a stable release or satisfy Mac binary signing/notarization gates. Candidate scope must be explicit; a beta does not imply every roadmap feature is complete.

The release notes must distinguish implemented features, automated test coverage, user-reported real-device behavior and unverified platforms. A 0.x release can be published as a normal release while explicitly documenting its early-product limits. Do not change repository privacy or select an open-source license implicitly.

A Mac binary requires the [signing and distribution acceptance](macos-signing.md).
Maintainers supply the Developer ID signing/notarization environment; end users
should not need signing credentials. Shared Swift tests alone do not satisfy that gate.

For Mac candidates, after committing aligned versions, use `scripts/package-macos.sh`
with a Developer ID fingerprint and an existing Keychain notarytool profile as
described in [maintainer packaging](macos-signing.md#maintainer-packaging).
It checks the committed source, builds/signs/notarizes, validates the final extracted
artifact, and writes an immutable candidate folder with provenance and checksums.
Mac candidates also include LICENSE, NOTICE.md, SOURCE.txt and source.zip from that exact commit.
It does not install the Host or publish a release. Pass native first-launch/update
acceptance before tagging and uploading. This pipeline has only been mocked here.
