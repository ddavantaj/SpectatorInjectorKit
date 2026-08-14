@echo off
chcp 65001 >nul
title 🎥 MATCH3 MR - SPECTATOR CAMERA INJECTOR KIT
cd /d "%~dp0"

echo ==================================================================
echo   🎥 UNITY MATCH3 MR - SPECTATOR CAMERA ^& PROXIMA INJECTOR
echo ==================================================================
echo.

if "%~1"=="" (
    InjectSpectatorCamera.exe
) else (
    InjectSpectatorCamera.exe "%~1"
)

pause
