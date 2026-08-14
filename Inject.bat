@echo off
chcp 65001 >nul
title 🌐 UNIVERSAL UNITY VR - SPECTATOR CAMERA INJECTOR
cd /d "%~dp0"

echo ==================================================================
echo   🌐 UNIVERSAL UNITY VR - SPECTATOR CAMERA ^& STUDIO SUITE INJECTOR
echo ==================================================================
echo.

if "%~1"=="" (
    InjectSpectatorCamera.exe
) else (
    InjectSpectatorCamera.exe "%~1"
)

pause
