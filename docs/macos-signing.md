# Mac signing and distribution plan

[简体中文](zh-CN/macos-signing.md) · [Mac guide](macos.md)

Status: v0.1.1 development. Update guards and startup tests are implemented;
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
