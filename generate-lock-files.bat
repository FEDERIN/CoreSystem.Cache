@echo off
setlocal

cd /d "%~dp0"

echo ==========================================
echo CoreSystem.Cache - NuGet Lock Files
echo ==========================================
echo.

echo [1/3] Restoring solution and updating lock files...
dotnet restore CoreSystem.Cache.sln --use-lock-file --force-evaluate

if errorlevel 1 (
    echo.
    echo ERROR: Failed to restore the solution.
    pause
    exit /b 1
)

echo.
echo [2/3] Lock files found:
echo.

for /r %%F in (packages.lock.json) do (
    echo %%F
)

echo.
echo [3/3] Checking Git status...
echo.

git status --short -- packages.lock.json

echo.
echo ==========================================
echo Lock files generation completed.
echo ==========================================
echo.

pause
endlocal