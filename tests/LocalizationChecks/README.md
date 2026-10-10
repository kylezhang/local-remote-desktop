# Shared UI language checks

Run the C# checks with an isolated `LOCALAPPDATA`/`APPDATA` test profile:

```powershell
dotnet run --project tests/LocalizationChecks/LocalizationChecks.csproj -c Release
```

The preference test writes only the language choice in that profile. It does not
read passwords or use the system clipboard. The same catalog is compiled into
the Windows application; actual bindings, tooltips and minimum-width layout are
covered by `tests/ClientChecks`.

Run the Foundation checks on a system with Swift:

```sh
bash tests/LocalizationChecks/check.sh
```

Both suites test every catalog entry and preserve opaque values in templates.
Foundation checks can run on Linux; they do not validate SwiftUI, macOS menus,
installed bundle resources or native user preferences. Native acceptance remains
required. See [interface conventions](../../docs/interface.md).
