@echo off
setlocal
title WuZhugeliang Telemetry Dashboard
cd /d "%~dp0"

rem Open the existing dashboard when the service is already running.
curl.exe --silent --show-error --fail --max-time 2 "http://127.0.0.1:4173/" >nul 2>&1
if not errorlevel 1 (
    echo Dashboard is already running at http://127.0.0.1:4173/
    start "" "http://127.0.0.1:4173/"
    exit /b 0
)

if not exist "dashboard\config.local.json" (
    echo First-time setup is required.
    echo.
    call npm.cmd run dashboard:setup
    if errorlevel 1 (
        echo.
        echo Setup failed. Check the message above and try again.
        pause
        exit /b 1
    )
    echo.
)

echo Starting the local telemetry dashboard...
echo Keep this window open. Press Ctrl+C here to stop it.
echo.
call npm.cmd run dashboard

if errorlevel 1 (
    echo.
    echo Dashboard startup failed. Check the message above.
    pause
)
