# Product rename compatibility checks

This Windows-only check loads the actual previous `LocalRemote.dll` and current
`LocalRemoteDesktop.dll` assemblies. It verifies old DPAPI data can still be read,
sharing credentials are not rotated, consent and language paths stay compatible,
and a completed legacy migration does not resurrect deleted data.

Set `LOCALREMOTE_PREVIOUS_ASSEMBLY` and `LOCALREMOTE_CURRENT_ASSEMBLY` to those DLLs,
and `LOCALREMOTE_NAMING_FIXTURE` to a fresh temporary directory whose name contains
`product-rename-compat-`. Run `dotnet run --project tests/NamingChecks -c Release`.
The prior assembly must be available locally; binaries are not committed.

All writes use explicit fixture paths. Windows known-folder APIs are not reliably
redirected by APPDATA/LOCALAPPDATA environment overrides. This check therefore
does not invoke default credential persistence or persist a language selection.
It uses synthetic credentials and does not read saved user passwords.

This is not native Mac Keychain/TCC migration acceptance.
