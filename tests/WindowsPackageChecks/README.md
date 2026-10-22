# Windows extraction and startup checks

From PowerShell 7 at the repository root:

```powershell
./tests/WindowsPackageChecks/check.ps1 -WorkDirectory "$env:TEMP/lrd-package-fixtures-new"
```

Use a new, short work directory. The fixtures check complete and partially extracted
packages, same-size file corruption, source mismatch, malformed/duplicate inventory
paths, omitted runtime records, deep paths and separately installed FFmpeg files.
They do not execute the fixture EXEs. The bundled verifier also works in Windows
PowerShell 5.1; the package writes its bilingual script with a UTF-8 BOM.

`scripts/ci/check-windows-archive.ps1` separately opens the actual final ZIP, requires
a flat app root, verifies isolated extracted bytes, and starts the real packaged EXE
in its explicit unshown diagnostic mode. A timeout or missing/failed JSON report
fails CI. The normal ClientChecks then use that extracted assembly. No real input,
desktop capture, clipboard, preferences or foreground window is used. These checks
do not establish interactive or two-device acceptance.

中文：使用新的较短隔离目录运行上方命令。模拟测试覆盖解压不完整、文件损坏、来源不符、
路径和清单错误，以及并排安装 FFmpeg 的兼容性；不会执行模拟 EXE。CI 另外解压最终 ZIP，
逐文件核验，以不显示窗口的诊断模式启动真实包内 EXE，并针对解压后的程序集执行客户端回归。
这些后台检查不代表真实两机操作验收通过。
