@echo off
chcp 65001 >nul
echo ================================================
echo Building MorePlayers Mod v1.0.4
echo ================================================
echo.

REM Path to MSBuild
set MSBUILD="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"

REM Check if MSBuild exists
if not exist %MSBUILD% (
    echo ERROR: MSBuild not found at %MSBUILD%
    echo Please install .NET Framework 4.7.2 or higher
    pause
    exit /b 1
)

REM Build the project
echo Building project...
%MSBUILD% TestMod.csproj /p:Configuration=Release /t:Build /v:minimal

REM Check build result
if %ERRORLEVEL% EQU 0 (
    echo.
    echo ================================================
    echo ✅ BUILD SUCCESS!
    echo ================================================
    echo Output: %CD%\Output\MorePlayers.dll
    echo.
    echo To install, copy to:
    echo E:\SteamLibrary\steamapps\common\MIMESIS\Mods\MorePlayers.dll
    echo.
) else (
    echo.
    echo ================================================
    echo ❌ BUILD FAILED!
    echo ================================================
    echo Check the errors above.
    echo.
)

pause
