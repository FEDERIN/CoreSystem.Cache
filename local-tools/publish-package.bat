@echo off
setlocal enabledelayedexpansion

rem ============================================================================
rem  CoreSystem.Cache - Publish a single package
rem ============================================================================
rem
rem  Usage:
rem      publish-package.bat <project> <version>
rem
rem    <project>   folder name under src\, one of:
rem                    Core.Cache
rem                    Core.Cache.Rehydration
rem                    Core.Cache.Redis
rem
rem    <version>   version to publish, e.g. 3.0.0. Must match the <Version>
rem                 already in the csproj; the tag is <project>/v<version> and
rem                 publish.yml re-reads the version from the tag.
rem
rem  This script does NOT publish. It verifies, then creates and pushes the
rem  tag, which is what triggers .github/workflows/publish.yml. Publishing is
rem  done by CI so it runs on ubuntu, not on this machine.
rem
rem  Published versions are immutable: a version already on nuget.org can never
rem  be replaced, and publish.yml has no --skip-duplicate, so a re-run of a
rem  live version fails with 409 instead of pretending to succeed. Check the
rem  version before running this.
rem ============================================================================

cd /d "%~dp0.."

set "SLN=CoreSystem.Cache.sln"
set "PROJECT=%~1"
set "VERSION=%~2"

echo ==========================================
echo CoreSystem.Cache - Publish
echo ==========================================
echo.

REM ---------------------------------------------------------------------------
REM  Validate arguments
REM ---------------------------------------------------------------------------
if "%PROJECT%"=="" (
    echo ERROR: missing project.
    echo.
    echo Usage: publish-package.bat ^<project^> ^<version^>
    echo.
    echo Projects: Core.Cache, Core.Cache.Rehydration, Core.Cache.Redis
    goto :fail
)

if "%VERSION%"=="" (
    echo ERROR: missing version.
    echo.
    echo Usage: publish-package.bat ^<project^> ^<version^>
    echo.
    echo Example: publish-package.bat Core.Cache 3.0.0
    goto :fail
)

set "CSPROJ=src\%PROJECT%\%PROJECT%.csproj"

if not exist "%CSPROJ%" (
    echo ERROR: '%CSPROJ%' does not exist.
    echo.
    echo The project argument is the folder name under src\, not the PackageId.
    echo Valid values: Core.Cache, Core.Cache.Rehydration, Core.Cache.Redis
    goto :fail
)

REM ---------------------------------------------------------------------------
REM  Map project to its test projects
REM ---------------------------------------------------------------------------
if /i "%PROJECT%"=="Core.Cache" (
    set "PACKAGE_ID=CoreSystem.Cache"
    set "TESTS=tests\Core.Cache.UnitTests\Core.Cache.UnitTests.csproj tests\Core.Cache.IntegrationTests\Core.Cache.IntegrationTests.csproj"
) else if /i "%PROJECT%"=="Core.Cache.Rehydration" (
    set "PACKAGE_ID=CoreSystem.Cache.Rehydration"
    set "TESTS=tests\Core.Cache.Rehydration.UnitTests\Core.Cache.Rehydration.UnitTests.csproj"
) else if /i "%PROJECT%"=="Core.Cache.Redis" (
    set "PACKAGE_ID=CoreSystem.Cache.Redis"
    set "TESTS=tests\Core.Cache.Redis.UnitTests\Core.Cache.Redis.UnitTests.csproj"
) else (
    echo ERROR: unknown project '%PROJECT%'.
    goto :fail
)

echo Project    : %PROJECT%
echo PackageId  : %PACKAGE_ID%
echo Version    : %VERSION%
echo Tag        : %PROJECT%/v%VERSION%
echo.

REM ---------------------------------------------------------------------------
REM  Verify the version in the csproj matches the argument
REM ---------------------------------------------------------------------------
set "CSPROJ_VERSION="
for /f "tokens=*" %%A in ('powershell -NoProfile -Command "([xml](Get-Content -Raw '%CSPROJ%')).Project.PropertyGroup.Version"') do (
    set "CSPROJ_VERSION=%%A"
)

if not "!CSPROJ_VERSION!"=="%VERSION%" (
    echo ERROR: version mismatch.
    echo   argument      : %VERSION%
    echo   csproj        : !CSPROJ_VERSION!
    echo.
    echo The csproj must already carry the version being published, because the
    echo package metadata ships inside the .nupkg. Update Version,
    echo AssemblyVersion, FileVersion and InformationalVersion first.
    goto :fail
)

echo [ok] csproj version matches.

REM ---------------------------------------------------------------------------
REM  Verify the version is not already published
REM ---------------------------------------------------------------------------
echo.
echo [1/8] Checking nuget.org for an existing %PACKAGE_ID% %VERSION%...

set "ALREADY_PUBLISHED="
for /f "usebackq delims=" %%V in (`powershell -NoProfile -Command "try { (Invoke-RestMethod 'https://api.nuget.org/v3-flatcontainer/!PACKAGE_ID!/index.json' -TimeoutSec 20).versions } catch { '' }"`) do (
    set "ALREADY_PUBLISHED=%%V"
)

echo !ALREADY_PUBLISHED! | findstr /c:"%VERSION%" >nul 2>&1
if not errorlevel 1 (
    echo.
    echo ERROR: %PACKAGE_ID% %VERSION% is ALREADY PUBLISHED.
    echo.
    echo Published versions are immutable. Choose a new version.
    goto :fail
)

echo       not published, OK.

REM ---------------------------------------------------------------------------
REM  Verify the tag does not already exist
REM ---------------------------------------------------------------------------
echo.
echo [2/8] Checking for an existing tag %PROJECT%/v%VERSION%...

git rev-parse "%PROJECT%/v%VERSION%" >nul 2>&1
if not errorlevel 1 (
    echo.
    echo ERROR: tag '%PROJECT%/v%VERSION%' already exists.
    goto :fail
)

echo       no existing tag, OK.

REM ---------------------------------------------------------------------------
REM  Verify the working tree and branch
REM ---------------------------------------------------------------------------
echo.
echo [3/8] Verifying the working tree...

set "DIRTY="
git status --porcelain > "%TEMP%\publish-status.txt" 2>nul
for /f "usebackq delims=" %%S in ("%TEMP%\publish-status.txt") do set "DIRTY=%%S"

if not "!DIRTY!"=="" (
    echo.
    echo ERROR: the working tree is not clean.
    echo.
    git status --short
    goto :fail
)

echo       clean, OK.

set "BRANCH="
for /f "usebackq delims=" %%B in (`git rev-parse --abbrev-ref HEAD`) do set "BRANCH=%%B"

if /i not "!BRANCH!"=="main" (
    echo.
    echo ERROR: you are on branch '!BRANCH!', not main.
    echo The tag must point at a merged state.
    goto :fail
)

echo       on main, OK.

REM ---------------------------------------------------------------------------
REM  Sync with origin
REM ---------------------------------------------------------------------------
echo.
echo [4/8] Syncing with origin...

git pull origin main
if errorlevel 1 goto :fail

REM ---------------------------------------------------------------------------
REM  Restore, clean and rebuild the single project
REM ---------------------------------------------------------------------------
echo.
echo [5/8] Restoring and rebuilding %PROJECT%...

dotnet restore "%CSPROJ%"
if errorlevel 1 goto :fail

dotnet clean "%CSPROJ%" | findstr /r /c:"." >nul
dotnet restore "%CSPROJ%"
if errorlevel 1 goto :fail

dotnet build "%CSPROJ%" -c Release --no-restore
if errorlevel 1 goto :fail

REM ---------------------------------------------------------------------------
REM  Run the tests for this package
REM ---------------------------------------------------------------------------
echo.
echo [6/8] Running tests...

for %%T in (%TESTS%) do (
    echo.
    echo       %%~nxT
    dotnet test --project "%%T" -c Release --no-restore
    if errorlevel 1 goto :fail
)

REM ---------------------------------------------------------------------------
REM  Confirm the package was produced with the expected contents
REM ---------------------------------------------------------------------------
echo.
echo [7/8] Verifying the built package...

set "NUPKG=src\%PROJECT%\bin\Release\%PACKAGE_ID%.%VERSION%.nupkg"

if not exist "%NUPKG%" (
    echo.
    echo ERROR: expected '%NUPKG%' but it was not produced.
    dir /b "src\%PROJECT%\bin\Release\*.nupkg"
    goto :fail
)

echo       %NUPKG% found, OK.

REM ---------------------------------------------------------------------------
REM  Create and push the tag, which triggers publish.yml
REM ---------------------------------------------------------------------------
echo.
echo [8/8] Creating and pushing the tag...

set "TAG=%PROJECT%/v%VERSION%"

git tag -a "!TAG!" -m "%PACKAGE_ID% %VERSION%"

if errorlevel 1 (
    echo.
    echo ERROR: failed to create the tag.
    goto :fail
)

git push origin "!TAG!"
if errorlevel 1 (
    echo.
    echo ERROR: failed to push the tag. The local tag was kept.
    echo Delete it with: git tag -d "!TAG!"
    goto :fail
)

echo.
echo ==========================================
echo Tag pushed: !TAG!
echo ==========================================
echo.
echo publish.yml is now running. Watch it with:
echo.
echo   gh run watch
echo.
echo Publish order is base-first, not reverse:
echo   Core.Cache -^> Core.Cache.Rehydration -^> Core.Cache.Redis
echo.
echo An unversioned ProjectReference becomes a nuspec dependency of the
echo referenced version, so publishing a dependent first would put a package
echo on the feed whose dependency does not exist yet.
echo.
echo The release is not resumable: publish.yml has no --skip-duplicate, so
echo re-running a tag that already published fails with 409 rather than
echo reporting success. A failure in the last tag leaves the earlier ones
echo live, and the recovery path is manual.
echo.
pause
exit /b 0

:fail
echo.
echo ==========================================
echo ABORTED - nothing was tagged or pushed.
echo ==========================================
echo.
pause
exit /b 1
