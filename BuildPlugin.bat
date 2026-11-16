@echo off
setlocal
cd /d "%~dp0"
set "GAME_MANAGED=D:\_AI\m3-ar_Build-win-26-11-12\m3-ar_Build-win\Match3MR_Data\Managed"
if not "%~1"=="" set "GAME_MANAGED=%~1"
if not exist "%GAME_MANAGED%\UnityEngine.CoreModule.dll" (
  echo Nu gasesc bibliotecile Unity in: %GAME_MANAGED%
  echo Folosire: BuildPlugin.bat "calea catre folderul Managed al jocului"
  pause
  exit /b 1
)
dotnet build SpectatorPlugin.csproj -c Release "-p:GameManaged=%GAME_MANAGED%"
if errorlevel 1 (
  echo Compilarea a esuat.
  pause
  exit /b 1
)
copy /Y "bin\Release\netstandard2.1\SpectatorPlugin.dll" "SpectatorPlugin.dll" >nul
if errorlevel 1 exit /b 1
echo Plugin compilat. Reinstaleaza-l cu Inject.bat dupa inchiderea jocului.
pause