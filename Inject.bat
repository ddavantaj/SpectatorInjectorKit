@echo off
chcp 65001 >nul
title 🎥 SPECTATOR CAMERA & 3D STUDIO INJECTOR (v3.1.1)
cd /d "%~dp0"

echo ==================================================================
echo   🎥 UNITY SPECTATOR CAMERA & 3D VIRTUAL STUDIO SUITE (v3.1.1)
echo ==================================================================
echo.

if "%~1"=="" (
    InjectSpectatorCamera.exe
) else (
    InjectSpectatorCamera.exe "%~1"
)

pause
