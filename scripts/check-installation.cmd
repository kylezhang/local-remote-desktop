@echo off
setlocal
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Verify-Package.ps1"
set "checkResult=%ERRORLEVEL%"
echo.
pause
exit /b %checkResult%
