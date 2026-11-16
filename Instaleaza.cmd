@echo off
rem Spectator Studio - dublu-click pentru a deschide installerul
cd /d "%~dp0"
powershell -NoProfile -Command "Get-ChildItem -LiteralPath '%~dp0' -Recurse -File | Unblock-File" >nul 2>&1
start "" powershell -NoProfile -ExecutionPolicy Bypass -STA -WindowStyle Hidden -File "%~dp0Installer.ps1"
