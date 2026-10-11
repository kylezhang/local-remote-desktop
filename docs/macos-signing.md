# Mac signing and distribution plan

[简体中文](zh-CN/macos-signing.md) · [Mac guide](macos.md)

Status: v0.3.0 development (2026-10-11). Update guards and startup tests are implemented;
there is no independently validated, signed Mac release package yet.

| Build | Intended use | Update expectations |
|---|---|---|
| Ad-hoc | Local development | Changed code can change its designated requirement; incompatible permission-preserving updates are rejected. |
| Apple Development | A developer's fixed Mac test environment | Pin an existing identity and verify requirements before replacement. |
| Developer ID Application | A downloaded product release | A maintained Mac signs, notarizes and validates the package. Users should not need their own certificate or local rebuild. |

Apple describes application identity in [TN3127](https://developer.apple.com/documentation/technotes/tn3127-inside-code-signing-requirements),
certificate policy in [TN2206](https://developer.apple.com/library/archive/technotes/tn2206/),
and [release notarization](https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution).
Development-to-distribution migration may need new approvals and must be tested.
Self-signed local identities can work for some policies, but TCC behavior has not
been verified here. No automatic trust-store modification is shipped.

The maintainer's next steps are:

1. Provide an Apple Silicon build/test machine and valid distribution identity.
   Keep private keys and credentials outside source, archives and logs.
2. Run `bash tests/MacStartupChecks/check.sh`. On Mac it also compiles the full
   Host with the Apple SDK, without signing/installing/opening it or resetting TCC.
3. Test first launch and two successive signed updates in a normal GUI session:
   unchanged pairing key, effective permissions, denial/cancellation without
   repeated automatic authentication. Shared logic tests do not prove system behavior.
4. Sign the final bundle consistently with release signing settings, notarize it,
   validate on a separate Mac user environment, and package with dependency notices,
   version/commit metadata and checksums.
5. Publish the verified artifact on an exact AtomGit release commit. Preserve old
   tags/assets. A future updater must verify downloaded artifacts and signing
   compatibility, failing before replacement when verification fails.

Screen Recording and Accessibility require user approval; an installer cannot
silently grant them. Stable signing preserves identity, not a guarantee of no
future macOS prompts. Current Windows/Ubuntu tests cover the Windows client and
shared Swift logic. Apple SDK, TCC, Keychain dialogs, capture and signing/notarization
remain native Mac acceptance requirements.

## Maintainer packaging

`scripts/package-macos.sh` implements the candidate build workflow. It requires
an Apple Silicon Mac, Xcode tools, Python 3, an existing valid Developer ID
Application identity and a notarytool credential profile stored in Keychain.
Prepare those on the maintainer's machine; do not put passwords, private keys or
certificate exports in the repository. End users do not run this pipeline.

After aligning the public version and committing the final source:

```bash
export SIGN_IDENTITY='<valid Developer ID Application fingerprint>'
export NOTARY_PROFILE='<existing notarytool Keychain profile name>'
bash scripts/package-macos.sh 0.3.0
```

The example version is the planned release, not the current app version. The committed app remains 0.3.0-alpha.2, so requesting a stable 0.3.0
package correctly fails until release preparation. A clean source tree and matching versions are required.

The pipeline compiles the full Host and runs startup checks, creates a fresh
bundle, signs with Hardened Runtime and a secure timestamp, checks the Developer
ID requirement and excludes debug entitlements. It submits an app ZIP with
`notarytool`, requires `Accepted`, staples and validates the ticket, and runs
Gatekeeper assessment. It then builds the final ZIP, checks its file inventory,
version, Apple Silicon executable, icons, commit and hashes, and extracts it for
another signature/ticket/assessment check. Apple describes these requirements in
[notarization requirements](https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution)
and the [custom workflow](https://developer.apple.com/documentation/security/customizing-the-notarization-workflow).

Outputs go into `dist/LocalRemoteDesktop-vVERSION-macos-arm64/`: ZIP, SHA256SUMS.txt and
release.json. The ZIP includes notices, a bilingual launch guide and file hashes.
Existing outputs are never overwritten; failed checks remove temporary candidates.
The script does not install/open the app, reset privacy permissions, read pairing
keys, change local signing pins, create tags or publish an AtomGit release.

Portable tests exercise package rejection and mocked Apple-tool failures; they
do not validate real signatures, notarization or permissions. No candidate has
been natively produced here. A successful pipeline is still not the first-launch
and two-update TCC/Keychain acceptance required for publishing a stable desktop release.
