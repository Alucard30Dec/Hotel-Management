@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build-installer.ps1" %*
if errorlevel 1 (
    echo.
    echo Build installer failed.
    exit /b %errorlevel%
)

echo.
echo Build installer completed.
