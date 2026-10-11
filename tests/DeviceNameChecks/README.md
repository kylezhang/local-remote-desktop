# Device-name checks

Run `dotnet run --project tests/DeviceNameChecks -c Release` and
`bash tests/DeviceNameChecks/check.sh` (Swift 5.9 or later).

These checks link the production name helpers and use isolated disposable
preferences. They cover save/relaunch, restore to system default, trimming,
Unicode bounds, invalid edits and optional authenticated metadata fallback.
They do not modify OS computer names, pairing credentials or user preferences.

Related checks: `ClientChecks` for unshown Windows editing/list refresh,
`WindowsHostChecks` for encrypted metadata/heartbeat propagation,
`MacHostModelChecks` for active rename and preserved sharing/password,
`MacControllerChecks` for session labels, and `DiscoveryChecks` for DNS-SD names.
Swift service doubles do not establish Apple SDK or native UI acceptance.
