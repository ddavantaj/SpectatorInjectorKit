@echo off
chcp 65001 >nul
title 🔨 BUILD SPECTATOR PLUGIN (v3.1.1)
cd /d "%~dp0"

echo ==================================================================
echo   🔨 COMPILARE SPECTATORPLUGIN.DLL (v3.1.1)
echo ==================================================================
echo.

if not exist "_build" mkdir "_build"

(
echo ^<Project Sdk="Microsoft.NET.Sdk"^>
echo   ^<PropertyGroup^>
echo     ^<TargetFramework^>netstandard2.1^</TargetFramework^>
echo     ^<AssemblyName^>SpectatorPlugin^</AssemblyName^>
echo     ^<OutputType^>Library^</OutputType^>
echo     ^<LangVersion^>9.0^</LangVersion^>
echo     ^<Nullable^>disable^</Nullable^>
echo     ^<Optimize^>true^</Optimize^>
echo   ^</PropertyGroup^>
echo   ^<ItemGroup^>
echo     ^<Compile Include="..\SpectatorCameraController.cs" /^>
echo   ^</ItemGroup^>
echo   ^<ItemGroup^>
echo     ^<Reference Include="UnityEngine.CoreModule"^>
echo       ^<HintPath^>C:\Program Files\Unity\Hub\Editor\2022.3.38f1\Editor\Data\Managed\UnityEngine\UnityEngine.CoreModule.dll^</HintPath^>
echo       ^<Private^>false^</Private^>
echo     ^</Reference^>
echo     ^<Reference Include="UnityEngine.IMGUIModule"^>
echo       ^<HintPath^>C:\Program Files\Unity\Hub\Editor\2022.3.38f1\Editor\Data\Managed\UnityEngine\UnityEngine.IMGUIModule.dll^</HintPath^>
echo       ^<Private^>false^</Private^>
echo     ^</Reference^>
echo     ^<Reference Include="UnityEngine.InputLegacyModule"^>
echo       ^<HintPath^>C:\Program Files\Unity\Hub\Editor\2022.3.38f1\Editor\Data\Managed\UnityEngine\UnityEngine.InputLegacyModule.dll^</HintPath^>
echo       ^<Private^>false^</Private^>
echo     ^</Reference^>
echo     ^<Reference Include="UnityEngine.AudioModule"^>
echo       ^<HintPath^>C:\Program Files\Unity\Hub\Editor\2022.3.38f1\Editor\Data\Managed\UnityEngine\UnityEngine.AudioModule.dll^</HintPath^>
echo       ^<Private^>false^</Private^>
echo     ^</Reference^>
echo     ^<Reference Include="UnityEngine.TextRenderingModule"^>
echo       ^<HintPath^>C:\Program Files\Unity\Hub\Editor\2022.3.38f1\Editor\Data\Managed\UnityEngine\UnityEngine.TextRenderingModule.dll^</HintPath^>
echo       ^<Private^>false^</Private^>
echo     ^</Reference^>
echo   ^</ItemGroup^>
echo ^</Project^>
) > "_build\SpectatorPlugin.csproj"

dotnet build "_build\SpectatorPlugin.csproj" -c Release
if %ERRORLEVEL% equ 0 (
    copy /Y "_build\bin\Release\netstandard2.1\SpectatorPlugin.dll" "SpectatorPlugin.dll" >nul
    rmdir /S /Q "_build" >nul 2>&1
    echo.
    echo [SUCCESS] SpectatorPlugin.dll a fost compilat si salvat cu succes!
) else (
    echo.
    echo [ERROR] Compilarea a esuat! Verificati erorile de mai sus.
)

echo.
pause
