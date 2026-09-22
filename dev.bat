@echo off
title Windows Startup Manager - Dev Tools
setlocal enabledelayedexpansion
cd /d "%~dp0"

set "SLN=WindowsStartupManager.sln"
set "EXE=%~dp0src\WindowsStartupManager.App\bin\Debug\net8.0-windows\WindowsStartupManager.exe"
set "PUBLISH_DIR=%~dp0publish"

:menu
cls
echo.
echo  ==========================================
echo      Windows Startup Manager  Dev Tools
echo  ==========================================
echo.
echo   1. Build + run
echo   2. Build + run as administrator
echo   3. Run tests
echo   4. Publish Release (self-contained single exe)
echo.
echo   0. Exit
echo.
set "choice="
set /p choice= Enter option:

if "%choice%"=="1" goto run
if "%choice%"=="2" goto run_admin
if "%choice%"=="3" goto test
if "%choice%"=="4" goto publish
if "%choice%"=="0" goto exit_program
echo  Invalid option, try again
timeout /t 1 >nul
goto menu

:build
echo.
echo  Building %SLN% ...
call dotnet build "%SLN%" -nologo -v q
if %errorlevel% neq 0 (
    echo.
    echo  [FAILED] Build failed. Close any running WindowsStartupManager.exe and retry.
    exit /b 1
)
exit /b 0

:run
cls
call :build
if %errorlevel% neq 0 (
    pause
    goto menu
)
echo.
echo  Launching app...
start "" "%EXE%"
timeout /t 2 >nul
goto menu

:run_admin
cls
call :build
if %errorlevel% neq 0 (
    pause
    goto menu
)
echo.
echo  Launching app as administrator (accept the UAC prompt)...
powershell -NoProfile -Command "Start-Process -FilePath '%EXE%' -Verb RunAs"
timeout /t 2 >nul
goto menu

:test
cls
echo.
echo  Running tests...
echo  ----------------------------------------
call dotnet test "%SLN%" -nologo
if %errorlevel% neq 0 (
    echo.
    echo  [FAILED] Some tests failed.
) else (
    echo.
    echo  [OK] All tests passed.
)
pause
goto menu

:publish
cls
echo.
echo  Publishing Release build to:
echo    %PUBLISH_DIR%
echo  ----------------------------------------
call dotnet publish "src\WindowsStartupManager.App\WindowsStartupManager.App.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%PUBLISH_DIR%" -nologo
if %errorlevel% neq 0 (
    echo.
    echo  [FAILED] Publish failed.
    pause
    goto menu
)
echo.
echo  [OK] Output: %PUBLISH_DIR%\WindowsStartupManager.exe
explorer "%PUBLISH_DIR%"
pause
goto menu

:exit_program
endlocal
exit /b 0
