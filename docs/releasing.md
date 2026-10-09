# Release process

[简体中文](zh-CN/releasing.md)

1. Decide scope and version. Product versions begin at 0.1.0; subsequent changes use semantic major/minor/patch intent. Keep Windows csproj, window title, Mac plist/UI and changelog aligned. Mac CFBundleVersion is a monotonically increasing build number even though the public version resets once from development 0.2.x.
2. Update English and Chinese documentation. Retain compatibility identities unless a tested migration is intentional. Inspect tracked files, `.gitignore`, diff/whitespace and secret indicators; do not commit dist/bin/obj/cache, logs or certificates.
3. Build Release on Windows, run protocol/client and affected script tests, and record actual results in validation. Native Mac compilation and UI/signing testing requires a Mac; explicitly state when absent.
4. Package Windows with .NET runtime licenses/notices and a short launch guide using `scripts/package-windows.ps1`. FFmpeg is external. Generate SHA-256 checksums. Verify the ZIP contains the tested client DLL, icon and Desktop runtime. Do not publish unverified Mac binaries.
5. Commit source/documents and push main to AtomGit. Use an annotated `vX.Y.Z` tag pointing at the exact release commit. Create the AtomGit Release, upload ZIP/checksum assets, then verify tag commit, release body and downloadable assets. Tags and release artifacts are immutable: fix mistakes in a new patch release rather than retargeting a tag.
6. The existing local Windows executable path remains a deployment target. Close only that instance before replacing it, then reopen one instance. Do not copy build output into Git or remove saved connections.

The release notes must distinguish implemented features, automated test coverage, user-reported real-device behavior and unverified platforms. A 0.x release can be published as a normal release while explicitly documenting its early-product limits. Do not change repository privacy or select an open-source license implicitly.

A Mac binary requires the [signing and distribution acceptance](macos-signing.md).
Maintainers supply the Developer ID signing/notarization environment; end users
should not need signing credentials. Shared Swift tests alone do not satisfy that gate.
