@echo off
rem Moka.Red - run the demos, the docs site, tests and tooling from one menu.
rem
rem Interactive:  RunSamples.bat
rem One-shot:     RunSamples.bat <choice> [option] [--no-browser] [--dry-run] [--no-color]
rem               RunSamples.bat devapp            hot reload, browser opens when ready
rem               RunSamples.bat test primitives
rem               RunSamples.bat docs build
rem Help:         RunSamples.bat help
rem
rem Takes the same kind of arguments as Moka.Docs\scripts\RunSamples.bat, so both
rem repos are driven the same way.

rem The browser opener relaunches this file in a minimized window. Dispatch it
rem before setlocal and the preflight, neither of which it needs.
if /i "%~1"=="__wait_open" goto :wait_open

setlocal EnableDelayedExpansion

set "SELF=%~f0"
for %%r in ("%~dp0..") do set "ROOT=%%~fr"
set "SLN=%ROOT%\Moka.Red.slnx"
set "DEVAPP=%ROOT%\samples\Moka.Red.DevApp"
set "WASMAPP=%ROOT%\samples\Moka.Red.WasmApp"
set "BENCH=%ROOT%\tests\Moka.Red.Benchmarks"
set "DOCS_DIR=%ROOT%\docs"
for %%r in ("%ROOT%\..\Moka.Docs\src\Moka.Docs.Cli") do set "DOCS_SRC=%%~fr"

set "DOCS_PORT=5080"
if defined MOKA_DOCS_PORT set "DOCS_PORT=%MOKA_DOCS_PORT%"
set "URL_DOCS=http://localhost:%DOCS_PORT%"
set "PACK_OUT=%ROOT%\nupkgs"
if defined MOKA_PACK_OUT set "PACK_OUT=%MOKA_PACK_OUT%"
set "DOCS_USE_SRC="
if defined MOKA_DOCS_LOCAL set "DOCS_USE_SRC=1"

set "ARG1=" & set "ARG2=" & set "ARG3="
set "DRY_RUN=" & set "NO_BROWSER=" & set "ONESHOT=" & set "BUILT=" & set "LAST_RC=0"

:parse_args
if "%~1"=="" goto :args_done
if /i "%~1"=="--dry-run"    (set "DRY_RUN=1"    & shift & goto :parse_args)
if /i "%~1"=="--no-browser" (set "NO_BROWSER=1" & shift & goto :parse_args)
if /i "%~1"=="--no-color"   (set "NO_COLOR=1"   & shift & goto :parse_args)
if not defined ARG1 (set "ARG1=%~1") else if not defined ARG2 (set "ARG2=%~1") else if not defined ARG3 (set "ARG3=%~1")
shift
goto :parse_args
:args_done

rem "utils port 5015" and "port 5015" mean the same thing. A bare "utils" opens
rem its menu instead.
if /i "%ARG1%"=="utils" if defined ARG2 (set "ARG1=%ARG2%" & set "ARG2=%ARG3%" & set "ARG3=")
if /i "%ARG1%"=="9" if defined ARG2 (set "ARG1=%ARG2%" & set "ARG2=%ARG3%" & set "ARG3=")
if defined ARG1 set "ONESHOT=1"

call :init_colors
if /i "%ARG1%"=="help"   goto :help
if /i "%ARG1%"=="-h"     goto :help
if /i "%ARG1%"=="--help" goto :help

call :preflight
if errorlevel 1 (
	set "LAST_RC=1"
	if not defined ONESHOT "%SystemRoot%\System32\timeout.exe" /t -1 >nul 2>&1
	goto :end
)
if defined ONESHOT goto :dispatch


rem ===========================================================================
rem  Menu
rem ===========================================================================

:menu
cls
call :banner
echo   %C_MUTED%RUN%C_OFF%
echo     %C_RED%1%C_OFF%  DevApp        %C_DIM%Blazor Server demo%C_OFF%            %C_MUTED%%URL_DEVAPP%%C_OFF%
echo     %C_RED%2%C_OFF%  WasmApp       %C_DIM%Blazor WebAssembly demo%C_OFF%       %C_MUTED%%URL_WASM%%C_OFF%
echo     %C_RED%3%C_OFF%  Docs          %C_DIM%mokadocs site, live previews%C_OFF%  %C_MUTED%%URL_DOCS%%C_OFF%
echo.
echo   %C_MUTED%BUILD AND VERIFY%C_OFF%
echo     %C_RED%4%C_OFF%  Build         %C_DIM%Debug, or Release with -warnaserror like CI%C_OFF%
echo     %C_RED%5%C_OFF%  Test          %C_DIM%all projects, one project, or a name filter%C_OFF%
echo     %C_RED%6%C_OFF%  Benchmarks    %C_DIM%BenchmarkDotNet in Release%C_OFF%
echo     %C_RED%7%C_OFF%  Pack          %C_DIM%NuGet packages, then checks the WebAssembly guard%C_OFF%
echo.
echo   %C_MUTED%TOOLS%C_OFF%
echo     %C_RED%8%C_OFF%  Clean         %C_DIM%bin, obj, node_modules, _site%C_OFF%
echo     %C_RED%9%C_OFF%  Utilities     %C_DIM%restore, build servers, ports, dev certificate, environment%C_OFF%
echo.
echo     %C_RED%0%C_OFF%  Exit
echo.
call :read_key 1234567890
if "%KEY_INDEX%"=="0"  goto :end
if "%KEY_INDEX%"=="1"  goto :devapp
if "%KEY_INDEX%"=="2"  goto :wasm
if "%KEY_INDEX%"=="3"  goto :docs
if "%KEY_INDEX%"=="4"  goto :build
if "%KEY_INDEX%"=="5"  goto :test
if "%KEY_INDEX%"=="6"  goto :bench
if "%KEY_INDEX%"=="7"  goto :pack
if "%KEY_INDEX%"=="8"  goto :clean
if "%KEY_INDEX%"=="9"  goto :utils
goto :end

:dispatch
if /i "%ARG1%"=="1"          goto :devapp
if /i "%ARG1%"=="devapp"     goto :devapp
if /i "%ARG1%"=="2"          goto :wasm
if /i "%ARG1%"=="wasm"       goto :wasm
if /i "%ARG1%"=="wasmapp"    goto :wasm
if /i "%ARG1%"=="3"          goto :docs
if /i "%ARG1%"=="docs"       goto :docs
if /i "%ARG1%"=="4"          goto :build
if /i "%ARG1%"=="build"      goto :build
if /i "%ARG1%"=="5"          goto :test
if /i "%ARG1%"=="test"       goto :test
if /i "%ARG1%"=="6"          goto :bench
if /i "%ARG1%"=="bench"      goto :bench
if /i "%ARG1%"=="benchmarks" goto :bench
if /i "%ARG1%"=="7"          goto :pack
if /i "%ARG1%"=="pack"       goto :pack
if /i "%ARG1%"=="8"          goto :clean
if /i "%ARG1%"=="clean"      goto :clean
if /i "%ARG1%"=="9"          goto :utils
if /i "%ARG1%"=="utils"      goto :utils
if /i "%ARG1%"=="restore"    goto :util_restore
if /i "%ARG1%"=="servers"    goto :util_servers
if /i "%ARG1%"=="port"       goto :util_port
if /i "%ARG1%"=="certs"      goto :util_certs
if /i "%ARG1%"=="info"       goto :util_info
echo.
echo   %C_RED%Unknown choice: !ARG1!%C_OFF%   run "RunSamples.bat help" for the list
set "LAST_RC=2"
goto :end


rem ===========================================================================
rem  1, 2  DevApp and WasmApp
rem ===========================================================================

:devapp
set "APP_NAME=DevApp"
set "APP_DESC=Blazor Server demo"
set "APP_PROJECT=%DEVAPP%"
set "APP_URL=%URL_DEVAPP%"
set "APP_HTTPS_URL="
goto :app

:wasm
set "APP_NAME=WasmApp"
set "APP_DESC=Blazor WebAssembly demo"
set "APP_PROJECT=%WASMAPP%"
set "APP_URL=%URL_WASM%"
set "APP_HTTPS_URL=%URL_WASM_HTTPS%"
goto :app

:app
set "MODE=%ARG2%"
if defined ONESHOT (
	if not defined MODE set "MODE=watch"
	goto :app_mode
)
cls
call :banner
echo   %C_BOLD%%APP_NAME%%C_OFF%  %C_DIM%%APP_DESC%%C_OFF%   %C_MUTED%%APP_URL%%C_OFF%
echo.
echo     %C_RED%W%C_OFF%  Watch         %C_DIM%hot reload, restarts on its own after edits it cannot apply%C_OFF%
echo     %C_RED%R%C_OFF%  Run           %C_DIM%dotnet run, Debug%C_OFF%
echo     %C_RED%L%C_OFF%  Release       %C_DIM%dotnet run -c Release%C_OFF%
set "APP_KEYS=WRLX"
if defined APP_HTTPS_URL (
	echo     %C_RED%S%C_OFF%  HTTPS         %C_DIM%watch with the https profile on !APP_HTTPS_URL!%C_OFF%
	set "APP_KEYS=WRLXS"
)
echo     %C_RED%X%C_OFF%  Back
echo.
call :read_key %APP_KEYS%
if "%KEY_INDEX%"=="0" goto :end
if "%KEY_INDEX%"=="4" goto :menu
if "%KEY_INDEX%"=="1" set "MODE=watch"
if "%KEY_INDEX%"=="2" set "MODE=run"
if "%KEY_INDEX%"=="3" set "MODE=release"
if "%KEY_INDEX%"=="5" set "MODE=https"

:app_mode
set "RUN_URL=%APP_URL%"
set "CMD="
if /i "%MODE%"=="watch"   set "CMD=dotnet watch --project "%APP_PROJECT%""
if /i "%MODE%"=="run"     set "CMD=dotnet run --project "%APP_PROJECT%""
if /i "%MODE%"=="release" set "CMD=dotnet run --project "%APP_PROJECT%" -c Release"
if /i "%MODE%"=="https" if defined APP_HTTPS_URL (
	set "RUN_URL=!APP_HTTPS_URL!"
	set "CMD=dotnet watch --project "!APP_PROJECT!" --launch-profile https"
)
if not defined CMD (
	echo.
	echo   %C_RED%!APP_NAME! has no "!MODE!" option.%C_OFF%   Choose watch, run or release.
	set "LAST_RC=2"
	goto :after
)

call :port_of "%RUN_URL%" RUN_PORT
call :resolve_port %RUN_PORT% "%RUN_URL%"
if errorlevel 1 goto :after

if /i "%MODE%"=="https" (
	dotnet dev-certs https --check --trust >nul 2>&1
	if errorlevel 1 (
		echo.
		echo   %C_WARN%The HTTPS development certificate is not trusted, so the browser will warn.%C_OFF%
		echo   %C_DIM%Trust it once with: dotnet dev-certs https --trust%C_OFF%
	)
)

rem A profile with launchBrowser set makes dotnet watch open the tab itself once
rem the app is up. Opening a second one would duplicate it, and suppressing
rem watch's launch also switches off its browser refresh, so only do that when
rem no browser was wanted at all.
set "WATCH_OPENS="
if /i "%MODE%"=="run"     goto :app_browser
if /i "%MODE%"=="release" goto :app_browser
findstr /i /r /c:"launchBrowser.: *true" "%APP_PROJECT%\Properties\launchSettings.json" >nul 2>&1
if not errorlevel 1 set "WATCH_OPENS=1"
:app_browser
call :ask_browser
if defined WATCH_OPENS (
	if not defined WANT_BROWSER set "DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER=1"
) else (
	if defined WANT_BROWSER call :open_when_ready "%RUN_URL%"
)
set "DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1"

echo.
echo   %C_BOLD%%APP_NAME%%C_OFF% on %C_RED%%RUN_URL%%C_OFF%
if defined WATCH_OPENS if defined WANT_BROWSER echo   %C_MUTED%dotnet watch opens the browser once the app is up%C_OFF%
if defined WATCH_OPENS if not defined WANT_BROWSER echo   %C_MUTED%browser launch suppressed, which also turns off dotnet watch's page refresh%C_OFF%
echo   %C_MUTED%Ctrl+C stops it, then answer N to come back to this menu%C_OFF%
call :exec
goto :after


rem ===========================================================================
rem  3  Docs (mokadocs)
rem ===========================================================================

:docs
<nul set /p "=%C_MUTED%  checking mokadocs...%C_OFF%"
call :docs_detect
echo.
if defined ONESHOT (
	set "DOCS_ACTION=%ARG2%"
	if not defined DOCS_ACTION set "DOCS_ACTION=serve"
	goto :docs_action
)

:docs_menu
cls
call :banner
call :docs_status_line
echo.
echo     %C_RED%S%C_OFF%  Serve         %C_DIM%build and serve with live reload%C_OFF%         %C_MUTED%%URL_DOCS%%C_OFF%
echo     %C_RED%B%C_OFF%  Build         %C_DIM%static site into docs\_site, with the CI base path%C_OFF%
echo     %C_RED%W%C_OFF%  Watch         %C_DIM%rebuild on every change, no server%C_OFF%
echo     %C_RED%V%C_OFF%  Validate      %C_DIM%dry-run build that lists every warning and error%C_OFF%
echo     %C_RED%D%C_OFF%  Doctor        %C_DIM%diagnose the docs project%C_OFF%
echo     %C_RED%U%C_OFF%  Update        %C_DIM%install or update the mokadocs global tool%C_OFF%
set "DOCS_KEYS=SBWVDUX"
if defined DOCS_HAS_SRC (
	echo     %C_RED%L%C_OFF%  Local source  %C_DIM%toggle running mokadocs from the Moka.Docs repo next to this one%C_OFF%
	set "DOCS_KEYS=SBWVDUXL"
)
echo     %C_RED%X%C_OFF%  Back
echo.
call :read_key %DOCS_KEYS%
if "%KEY_INDEX%"=="0" goto :end
if "%KEY_INDEX%"=="7" goto :menu
if "%KEY_INDEX%"=="8" goto :docs_toggle_local
set "DOCS_ACTION="
if "%KEY_INDEX%"=="1" set "DOCS_ACTION=serve"
if "%KEY_INDEX%"=="2" set "DOCS_ACTION=build"
if "%KEY_INDEX%"=="3" set "DOCS_ACTION=watch"
if "%KEY_INDEX%"=="4" set "DOCS_ACTION=validate"
if "%KEY_INDEX%"=="5" set "DOCS_ACTION=doctor"
if "%KEY_INDEX%"=="6" set "DOCS_ACTION=update"

:docs_action
if /i "%DOCS_ACTION%"=="update" goto :docs_update
if /i "%DOCS_ACTION%"=="local"  goto :docs_toggle_local
call :docs_exe
if errorlevel 1 goto :after_fail
if /i "%DOCS_ACTION%"=="serve"    goto :docs_serve
if /i "%DOCS_ACTION%"=="build"    goto :docs_build
if /i "%DOCS_ACTION%"=="watch"    goto :docs_watch
if /i "%DOCS_ACTION%"=="validate" goto :docs_validate
if /i "%DOCS_ACTION%"=="doctor"   goto :docs_doctor
echo.
echo   %C_RED%Unknown docs option: !DOCS_ACTION!%C_OFF%   Choose serve, build, watch, validate, doctor, update or local.
set "LAST_RC=2"
goto :after

rem CI builds the solution before mokadocs runs, so build, validate and doctor do too and
rem see what CI sees. Previews compile against preview-host's own bin, not this build.
:docs_serve
call :ensure_build
if defined BUILD_FAILED goto :after_fail
call :resolve_port %DOCS_PORT% "%URL_DOCS%"
if errorlevel 1 goto :after
call :ask_browser
echo.
echo   %C_BOLD%Docs%C_OFF% on %C_RED%%URL_DOCS%%C_OFF%   %C_MUTED%the first run publishes the Blazor preview host, so give it a minute%C_OFF%
echo   %C_MUTED%Ctrl+C stops the server, then answer N to come back to this menu%C_OFF%
if defined WANT_BROWSER call :open_when_ready "%URL_DOCS%"
set "CMD=%DOCS_EXE% serve --port %DOCS_PORT% --no-open"
pushd "%DOCS_DIR%"
call :exec
popd
goto :after

:docs_build
call :ensure_build
if defined BUILD_FAILED goto :after_fail
set "CMD=%DOCS_EXE% build --base-path /Moka.Red/"
pushd "%DOCS_DIR%"
call :exec
popd
goto :after

:docs_watch
set "CMD=%DOCS_EXE% build --watch"
pushd "%DOCS_DIR%"
call :exec
popd
goto :after

rem mokadocs 1.7.0 made both of these run a full dry-run build, previews included.
:docs_validate
call :ensure_build
if defined BUILD_FAILED goto :after_fail
set "CMD=%DOCS_EXE% validate"
pushd "%DOCS_DIR%"
call :exec
popd
call :docs_verdict
goto :after

:docs_doctor
call :ensure_build
if defined BUILD_FAILED goto :after_fail
set "CMD=%DOCS_EXE% doctor"
pushd "%DOCS_DIR%"
call :exec
popd
call :docs_verdict
goto :after

rem validate and doctor exit 0 when clean, 1 for warnings only and 2 for errors.
:docs_verdict
if defined DRY_RUN exit /b 0
if "%LAST_RC%"=="1" echo   %C_MUTED%exit 1 means warnings only, mokadocs build still succeeds%C_OFF%
if "%LAST_RC%"=="2" echo   %C_RED%exit 2 means errors, mokadocs build fails on them too%C_OFF%
exit /b 0

:docs_update
if defined DOCS_VER (
	set "CMD=dotnet tool update --global mokadocs"
) else (
	set "CMD=dotnet tool install --global mokadocs"
)
call :exec
set "DOCS_LATEST_CHECKED="
goto :after

:docs_toggle_local
if not defined DOCS_HAS_SRC goto :docs_no_src
if defined DOCS_USE_SRC (set "DOCS_USE_SRC=") else (set "DOCS_USE_SRC=1")
if defined ONESHOT goto :end
goto :docs_menu

:docs_no_src
echo.
echo   %C_RED%The Moka.Docs source was not found.%C_OFF%
echo   %C_DIM%Expected the CLI project at %DOCS_SRC%%C_OFF%
set "LAST_RC=2"
goto :after


rem ===========================================================================
rem  4  Build
rem ===========================================================================

:build
if defined ONESHOT goto :build_oneshot
cls
call :banner
echo   %C_BOLD%BUILD%C_OFF%  %C_DIM%Moka.Red.slnx%C_OFF%
echo.
echo     %C_RED%D%C_OFF%  Debug
echo     %C_RED%R%C_OFF%  Release       %C_DIM%-warnaserror, the same build CI runs%C_OFF%
echo     %C_RED%X%C_OFF%  Back
echo.
call :read_key DRX
if "%KEY_INDEX%"=="0" goto :end
if "%KEY_INDEX%"=="1" goto :build_debug
if "%KEY_INDEX%"=="2" goto :build_release
goto :menu

:build_oneshot
if not defined ARG2        goto :build_debug
if /i "%ARG2%"=="debug"    goto :build_debug
if /i "%ARG2%"=="release"  goto :build_release
echo.
echo   %C_RED%Unknown build option: !ARG2!%C_OFF%   Choose debug or release.
set "LAST_RC=2"
goto :end

:build_debug
set "CMD=dotnet build "%SLN%" -c Debug"
call :exec
if not errorlevel 1 if not defined DRY_RUN set "BUILT=1"
goto :after

:build_release
set "CMD=dotnet build "%SLN%" -c Release -warnaserror"
call :exec
goto :after


rem ===========================================================================
rem  5  Test
rem ===========================================================================

:test
call :collect_tests
if defined ONESHOT goto :test_oneshot

:test_menu
cls
call :banner
echo   %C_BOLD%TEST%C_OFF%  %C_DIM%%TEST_COUNT% projects, xunit v3 on Microsoft.Testing.Platform%C_OFF%
echo.
echo     %C_RED%A%C_OFF%  All projects
echo     %C_RED%F%C_OFF%  Filter        %C_DIM%tests whose name contains some text, across every project%C_OFF%
echo.
rem Keys come from a pool that skips A, F and X, so new test projects get a key
rem without the menu needing an edit.
set "TEST_KEYS=AFX"
set "KEY_POOL=123456789BCDEGHIJKLMNOPQRSTUVWYZ"
for /l %%i in (1,1,%TEST_COUNT%) do (
	set /a "KP=%%i-1"
	for %%k in (!KP!) do set "TK=!KEY_POOL:~%%k,1!"
	set "TEST_KEYS=!TEST_KEYS!!TK!"
	echo     %C_RED%!TK!%C_OFF%  !TEST_%%i!
)
echo.
echo     %C_RED%X%C_OFF%  Back
echo.
call :read_key %TEST_KEYS%
if "%KEY_INDEX%"=="0" goto :end
if "%KEY_INDEX%"=="1" goto :test_all
if "%KEY_INDEX%"=="2" goto :test_filter_prompt
if "%KEY_INDEX%"=="3" goto :menu
set /a "TEST_PICK=KEY_INDEX-3"
goto :test_one

:test_oneshot
if not defined ARG2 goto :test_all
if /i "%ARG2%"=="all" goto :test_all
if /i "%ARG2:~0,7%"=="filter:" (
	set "TEST_FILTER=!ARG2:~7!"
	goto :test_filter
)
rem Anything else picks the first project whose name contains it.
set "TEST_PICK="
for /l %%i in (1,1,%TEST_COUNT%) do (
	if not defined TEST_PICK (
		set "TN=!TEST_%%i!"
		if /i not "!TN:%ARG2%=!"=="!TN!" set "TEST_PICK=%%i"
	)
)
if defined TEST_PICK goto :test_one
echo.
echo   %C_RED%No test project matches "!ARG2!".%C_OFF%
set "LAST_RC=2"
goto :end

:test_all
set "CMD=dotnet test --solution "%SLN%""
call :exec
goto :after

:test_one
set "TEST_NAME=!TEST_%TEST_PICK%!"
set "CMD=dotnet test --project "%ROOT%\tests\%TEST_NAME%""
call :exec
goto :after

:test_filter_prompt
echo.
set "TEST_FILTER="
set /p "TEST_FILTER=%C_MUTED%  test name contains%C_OFF% %C_RED%>%C_OFF% "
if not defined TEST_FILTER goto :test_menu

rem A filter that matches nothing in a project makes that project exit 8, which
rem would fail the whole run, so that code is ignored.
:test_filter
set "CMD=dotnet test --solution "%SLN%" --filter-method "*%TEST_FILTER%*" --ignore-exit-code 8"
call :exec
goto :after


rem ===========================================================================
rem  6  Benchmarks
rem ===========================================================================

:bench
if defined ONESHOT (
	if /i "%ARG2%"=="pick" goto :bench_pick
	goto :bench_all
)
cls
call :banner
echo   %C_BOLD%BENCHMARKS%C_OFF%  %C_DIM%BenchmarkDotNet, Release, ShortRun jobs%C_OFF%
echo.
echo     %C_RED%A%C_OFF%  All           %C_DIM%every benchmark, takes a few minutes%C_OFF%
echo     %C_RED%P%C_OFF%  Pick          %C_DIM%BenchmarkDotNet's own selector%C_OFF%
echo     %C_RED%X%C_OFF%  Back
echo.
call :read_key APX
if "%KEY_INDEX%"=="0" goto :end
if "%KEY_INDEX%"=="1" goto :bench_all
if "%KEY_INDEX%"=="2" goto :bench_pick
goto :menu

:bench_all
set "CMD=dotnet run --project "%BENCH%" -c Release -- --filter "*""
call :exec
goto :after

:bench_pick
set "CMD=dotnet run --project "%BENCH%" -c Release"
call :exec
goto :after


rem ===========================================================================
rem  7  Pack
rem ===========================================================================

:pack
set "PACK_EXISTING=0"
for %%f in ("%PACK_OUT%\*.%VERSION%.nupkg") do set /a "PACK_EXISTING+=1"
if not defined ONESHOT (
	cls
	call :banner
)
echo   %C_BOLD%PACK%C_OFF%  %C_DIM%version %VERSION% into !PACK_OUT!%C_OFF%
if %PACK_EXISTING% gtr 0 echo   %C_WARN%%PACK_EXISTING% existing %VERSION% packages there will be replaced.%C_OFF%
if defined ONESHOT goto :pack_go
echo.
<nul set /p "=%C_MUTED%  continue?%C_OFF% %C_RED%[Y/n]%C_OFF% "
choice /c yn /n 2>nul
if errorlevel 2 goto :menu

:pack_go
set "CMD=dotnet pack "%SLN%" -c Release -o "%PACK_OUT%""
call :exec
if errorlevel 1 goto :after_fail
if defined DRY_RUN goto :after
call :verify_wasm_guard
if errorlevel 1 goto :after_fail
goto :after


rem ===========================================================================
rem  8  Clean
rem ===========================================================================

:clean
if defined ONESHOT (
	if /i "%ARG2%"=="dry"   goto :clean_dry
	if /i "%ARG2%"=="force" goto :clean_force
	goto :clean_run
)
cls
call :banner
echo   %C_BOLD%CLEAN%C_OFF%  %C_DIM%bin, obj, node_modules, _site, _sample-site%C_OFF%
echo.
echo     %C_RED%D%C_OFF%  Dry run       %C_DIM%list what would be deleted%C_OFF%
echo     %C_RED%C%C_OFF%  Clean         %C_DIM%stops the build servers first so nothing is locked, then asks%C_OFF%
echo     %C_RED%X%C_OFF%  Back
echo.
call :read_key DCX
if "%KEY_INDEX%"=="0" goto :end
if "%KEY_INDEX%"=="1" goto :clean_dry
if "%KEY_INDEX%"=="2" goto :clean_run
goto :menu

:clean_dry
set "CMD=powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\scripts\ClearProjectsCache.ps1" -DryRun"
call :exec
goto :after

:clean_run
set "CMD=dotnet build-server shutdown"
call :exec
set "CMD=powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\scripts\ClearProjectsCache.ps1""
call :exec
set "BUILT="
goto :after

:clean_force
set "CMD=dotnet build-server shutdown"
call :exec
set "CMD=powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\scripts\ClearProjectsCache.ps1" -Force"
call :exec
set "BUILT="
goto :after


rem ===========================================================================
rem  9  Utilities
rem ===========================================================================

:utils
cls
call :banner
echo   %C_BOLD%UTILITIES%C_OFF%
echo.
echo     %C_RED%R%C_OFF%  Restore           %C_DIM%dotnet restore%C_OFF%
echo     %C_RED%S%C_OFF%  Build servers     %C_DIM%shut them down to release locked bin and obj files%C_OFF%
echo     %C_RED%P%C_OFF%  Port              %C_DIM%see who is listening on a port, and stop it%C_OFF%
echo     %C_RED%C%C_OFF%  Dev certificate   %C_DIM%is the HTTPS development certificate trusted%C_OFF%
echo     %C_RED%I%C_OFF%  Environment       %C_DIM%SDKs, runtimes and tools%C_OFF%
echo     %C_RED%X%C_OFF%  Back
echo.
call :read_key RSPCIX
if "%KEY_INDEX%"=="0" goto :end
if "%KEY_INDEX%"=="1" goto :util_restore
if "%KEY_INDEX%"=="2" goto :util_servers
if "%KEY_INDEX%"=="3" goto :util_port
if "%KEY_INDEX%"=="4" goto :util_certs
if "%KEY_INDEX%"=="5" goto :util_info
if defined ONESHOT goto :end
goto :menu

:util_restore
set "CMD=dotnet restore "%SLN%""
call :exec
goto :after

:util_servers
set "CMD=dotnet build-server shutdown"
call :exec
set "BUILT="
goto :after

:util_port
set "PORT_Q=%ARG2%"
if defined PORT_Q goto :util_port_check
echo.
set /p "PORT_Q=%C_MUTED%  port%C_OFF% %C_RED%>%C_OFF% "
if not defined PORT_Q goto :after
:util_port_check
echo(!PORT_Q!| findstr /r /x "[0-9][0-9]*" >nul
if errorlevel 1 (
	echo.
	echo   %C_RED%Not a port number: !PORT_Q!%C_OFF%
	set "LAST_RC=2"
	goto :after
)
call :port_owner %PORT_Q%
if errorlevel 1 (
	echo.
	echo   %C_GREEN%Port %PORT_Q% is free.%C_OFF%
	goto :after
)
echo.
echo   Port %PORT_Q% is in use by %C_BOLD%%OWNER_NAME%%C_OFF%, PID %OWNER_PID%.
if defined ONESHOT goto :after
<nul set /p "=%C_MUTED%  stop it?%C_OFF% %C_RED%[y/N]%C_OFF% "
choice /c yn /n /t 15 /d n 2>nul
if errorlevel 2 goto :after
call :kill_pid %OWNER_PID% %PORT_Q%
goto :after

:util_certs
set "CMD=dotnet dev-certs https --check --trust"
call :exec
if errorlevel 1 (
	echo.
	echo   %C_WARN%The HTTPS development certificate is missing or not trusted.%C_OFF%
	echo   %C_DIM%The WasmApp https profile needs it. Create and trust it with: dotnet dev-certs https --trust%C_OFF%
	set "LAST_RC=0"
) else (
	echo.
	echo   %C_GREEN%The HTTPS development certificate is trusted.%C_OFF%
)
goto :after

:util_info
<nul set /p "=%C_MUTED%  checking mokadocs...%C_OFF%"
call :docs_detect
echo.
echo.
echo   %C_MUTED%REPO%C_OFF%       %ROOT%
echo   %C_MUTED%VERSION%C_OFF%    %VERSION%
echo   %C_MUTED%BRANCH%C_OFF%     %BRANCH%
echo   %C_MUTED%SDK%C_OFF%        %SDK_VER%   %C_DIM%resolved through global.json%C_OFF%
echo.
echo   %C_MUTED%INSTALLED SDKS%C_OFF%
for /f "delims=" %%s in ('dotnet --list-sdks') do echo     %%s
echo.
echo   %C_MUTED%ASP.NET CORE RUNTIMES%C_OFF%
for /f "delims=" %%s in ('dotnet --list-runtimes ^| findstr /b /c:"Microsoft.AspNetCore.App"') do echo     %%s
echo.
set "INFO_DOCS=%C_WARN%not installed%C_OFF%"
if defined DOCS_VER set "INFO_DOCS=%DOCS_VER%"
set "INFO_LATEST=unknown, offline"
if defined DOCS_LATEST set "INFO_LATEST=%DOCS_LATEST%"
set "INFO_SRC=not found"
if defined DOCS_HAS_SRC set "INFO_SRC=%DOCS_SRC%"
echo   %C_MUTED%MOKADOCS%C_OFF%   global tool %INFO_DOCS%   %C_DIM%latest on NuGet %INFO_LATEST%%C_OFF%
echo   %C_MUTED%MOKA.DOCS%C_OFF%  source !INFO_SRC!
set "INFO_CURL=not found, falling back to netstat"
where curl.exe >nul 2>&1 && set "INFO_CURL=available"
echo   %C_MUTED%CURL%C_OFF%       %INFO_CURL%   %C_DIM%used to open the browser once an app responds%C_OFF%
goto :after


rem ===========================================================================
rem  Flow
rem ===========================================================================

:after
rem timeout /t -1 rather than pause: both wait for any key at a console, but when
rem input is piped pause can hit end of input and leave the next choice blocked
rem forever, while timeout refuses redirected input and returns straight away.
if defined ONESHOT goto :end
echo.
<nul set /p "=%C_MUTED%  press any key to return to the menu%C_OFF% "
"%SystemRoot%\System32\timeout.exe" /t -1 >nul 2>&1
echo.
goto :menu

:after_fail
if "%LAST_RC%"=="0" set "LAST_RC=1"
if defined ONESHOT goto :end
echo.
echo   %C_RED%That did not succeed.%C_OFF%   See the output above.
<nul set /p "=%C_MUTED%  press any key to return to the menu%C_OFF% "
"%SystemRoot%\System32\timeout.exe" /t -1 >nul 2>&1
echo.
goto :menu

:end
set "END_RC=%LAST_RC%"
endlocal & exit /b %END_RC%

:help
echo.
echo   %C_RED%%C_BOLD%MOKA.RED%C_OFF%  %C_DIM%RunSamples.bat%C_OFF%
echo.
echo   %C_MUTED%USAGE%C_OFF%
echo     RunSamples.bat                                interactive menu
echo     RunSamples.bat ^<choice^> [option] [flags]      run one thing and exit
echo.
echo   %C_MUTED%CHOICES%C_OFF%
echo     1  devapp   watch ^| run ^| release                      default watch
echo     2  wasm     watch ^| run ^| release ^| https              default watch
echo     3  docs     serve ^| build ^| watch ^| validate ^| doctor  default serve
echo                 update ^| local
echo     4  build    debug ^| release                            default debug
echo     5  test     all ^| ^<project^> ^| filter:^<text^>            default all
echo     6  bench    all ^| pick                                 default all
echo     7  pack
echo     8  clean    dry ^| force                                default asks first
echo     9  utils    restore ^| servers ^| port ^<n^> ^| certs ^| info
echo.
echo   %C_MUTED%FLAGS%C_OFF%
echo     --no-browser     never open a browser
echo     --dry-run        print each command instead of running it
echo     --no-color       plain output, same as setting NO_COLOR
echo.
echo   %C_MUTED%ENVIRONMENT%C_OFF%
echo     MOKA_PACK_OUT    where pack writes packages, default nupkgs
echo     MOKA_DOCS_PORT   docs server port, default 5080
echo     MOKA_DOCS_LOCAL  set to run mokadocs from ..\Moka.Docs source
echo.
echo   %C_MUTED%EXAMPLES%C_OFF%
echo     RunSamples.bat devapp
echo     RunSamples.bat wasm https --no-browser
echo     RunSamples.bat test primitives
echo     RunSamples.bat test filter:Barcode
echo     RunSamples.bat docs update
echo     RunSamples.bat port 5015
echo.
set "LAST_RC=0"
goto :end


rem ===========================================================================
rem  Helpers
rem ===========================================================================

:init_colors
set "C_RED=" & set "C_GREEN=" & set "C_WARN=" & set "C_DIM=" & set "C_MUTED=" & set "C_BOLD=" & set "C_OFF="
if defined NO_COLOR exit /b 0
for /f %%a in ('echo prompt $E^| cmd') do set "ESC=%%a"
rem Moka palette: #ef5350 accent, #00e676 ok, #ffab40 warning, #a0a0aa and #6a6a74 text.
set "C_RED=!ESC![38;2;239;83;80m"
set "C_GREEN=!ESC![38;2;0;230;118m"
set "C_WARN=!ESC![38;2;255;171;64m"
set "C_DIM=!ESC![38;2;160;160;170m"
set "C_MUTED=!ESC![38;2;106;106;116m"
set "C_BOLD=!ESC![1m"
set "C_OFF=!ESC![0m"
exit /b 0

:preflight
where dotnet >nul 2>&1
if errorlevel 1 (
	echo.
	echo   %C_RED%dotnet was not found on PATH.%C_OFF%
	echo   Install the .NET 10 SDK from https://dot.net and open a new terminal.
	exit /b 1
)
pushd "%ROOT%"
dotnet --version >nul 2>&1
set "SDK_RC=%errorlevel%"
set "SDK_VER="
if "%SDK_RC%"=="0" for /f "delims=" %%v in ('dotnet --version') do set "SDK_VER=%%v"
popd
if not "%SDK_RC%"=="0" (
	echo.
	echo   %C_RED%No installed .NET SDK satisfies global.json.%C_OFF%
	echo   Moka.Red pins 10.0.201 with rollForward latestFeature, so any 10.0 SDK from 10.0.201 up works.
	exit /b 1
)
set "VERSION="
for /f "tokens=3 delims=<>" %%v in ('findstr /c:"<Version>" "%ROOT%\Directory.Build.targets" 2^>nul') do if not defined VERSION set "VERSION=%%v"
if not defined VERSION set "VERSION=unknown"
set "BRANCH="
for /f "delims=" %%b in ('git -C "%ROOT%" rev-parse --abbrev-ref HEAD 2^>nul') do set "BRANCH=%%b"
if not defined BRANCH set "BRANCH=n/a"
call :read_urls "%DEVAPP%\Properties\launchSettings.json" URL_DEVAPP URL_DEVAPP_HTTPS
call :read_urls "%WASMAPP%\Properties\launchSettings.json" URL_WASM URL_WASM_HTTPS
if not defined URL_DEVAPP set "URL_DEVAPP=http://localhost:5015"
if not defined URL_WASM set "URL_WASM=http://localhost:5240"
exit /b 0

:banner
echo.
echo   %C_RED%%C_BOLD%MOKA.RED%C_OFF%  %C_DIM%samples, docs and tooling%C_OFF%
echo   %C_MUTED%---------------------------------------------------------------------------%C_OFF%
set "BANNER_BUILD=%C_MUTED%not built this session%C_OFF%"
if defined BUILT set "BANNER_BUILD=%C_GREEN%built this session%C_OFF%"
set "BANNER_FLAGS="
if defined DRY_RUN set "BANNER_FLAGS=   %C_WARN%dry run, nothing will execute%C_OFF%"
echo   %C_MUTED%v%C_OFF%%VERSION%   %C_MUTED%sdk%C_OFF% %SDK_VER%   %C_MUTED%branch%C_OFF% %BRANCH%   %BANNER_BUILD%%BANNER_FLAGS%
echo.
exit /b 0

rem Single keypress. Sets KEY_INDEX to the key's 1-based position, or 0 when input
rem runs out (a piped run, a closed console), so every menu treats that as exit
rem instead of spinning forever.
:read_key
<nul set /p "=%C_MUTED%  select%C_OFF% %C_RED%>%C_OFF% "
choice /c %~1 /n 2>nul
set "KEY_INDEX=%errorlevel%"
if "%KEY_INDEX%"=="255" set "KEY_INDEX=0"
exit /b 0

rem Runs CMD, echoing it first so what ran is never a mystery.
:exec
echo.
echo   %C_MUTED%^>%C_OFF% %C_DIM%!CMD!%C_OFF%
if defined DRY_RUN (
	set "LAST_RC=0"
	exit /b 0
)
%CMD%
set "LAST_RC=%errorlevel%"
exit /b %LAST_RC%

rem Builds once per session. Sets BUILD_FAILED rather than jumping, because a
rem goto out of a called routine leaves the call stack unbalanced.
:ensure_build
set "BUILD_FAILED="
if defined BUILT exit /b 0
echo.
echo   %C_DIM%Building the solution, first time this session%C_OFF%
set "CMD=dotnet build "%SLN%" -c Debug -v minimal"
call :exec
if errorlevel 1 (
	set "BUILD_FAILED=1"
	exit /b 1
)
if not defined DRY_RUN set "BUILT=1"
exit /b 0

rem Reads the first http and https URL out of a launchSettings.json. Ports live
rem there, so reading them keeps this script right when someone changes one.
:read_urls
set "RU_HTTP=" & set "RU_HTTPS="
if not exist "%~1" goto :read_urls_done
for /f tokens^=4^ delims^=^" %%u in ('findstr /c:"applicationUrl" "%~1"') do (
	set "RU_LIST=%%u"
	for %%p in ("!RU_LIST:;=" "!") do (
		set "RU_ONE=%%~p"
		if /i "!RU_ONE:~0,8!"=="https://" (
			if not defined RU_HTTPS set "RU_HTTPS=!RU_ONE!"
		) else if /i "!RU_ONE:~0,7!"=="http://" (
			if not defined RU_HTTP set "RU_HTTP=!RU_ONE!"
		)
	)
)
:read_urls_done
set "%~2=!RU_HTTP!"
set "%~3=!RU_HTTPS!"
exit /b 0

:port_of
set "%~2="
for /f "tokens=3 delims=:/" %%p in ("%~1") do set "%~2=%%p"
exit /b 0

rem Sets OWNER_PID and OWNER_NAME for whatever is listening on a TCP port.
rem Exit code 1 means the port is free.
:port_owner
set "OWNER_PID=" & set "OWNER_NAME="
for /f "tokens=5" %%p in ('netstat -ano 2^>nul ^| findstr /r /c:":%~1 .*LISTENING"') do if not defined OWNER_PID set "OWNER_PID=%%p"
if not defined OWNER_PID exit /b 1
rem find.exe and PING.EXE are pinned to System32 because Git for Windows and MSYS2
rem can put Unix tools ahead of it on PATH. Unix find treats "," as a directory,
rem and Unix ping -n means numeric output, so it would ping forever.
for /f "tokens=1 delims=," %%n in ('tasklist /fi "PID eq %OWNER_PID%" /nh /fo csv 2^>nul ^| "%SystemRoot%\System32\find.exe" ","') do set "OWNER_NAME=%%~n"
if not defined OWNER_NAME set "OWNER_NAME=an unknown process"
exit /b 0

rem Exit code 0 means the port is free, or was freed, and the caller should go on.
:resolve_port
call :port_owner %~1
if errorlevel 1 exit /b 0
echo.
echo   %C_WARN%Port %~1 is already in use by %OWNER_NAME%, PID %OWNER_PID%.%C_OFF%
echo.
echo     %C_RED%O%C_OFF%  Open %~2      %C_DIM%it is probably already running%C_OFF%
echo     %C_RED%K%C_OFF%  Stop that process and start fresh
echo     %C_RED%X%C_OFF%  Cancel
echo.
call :read_key OKX
if "%KEY_INDEX%"=="1" goto :resolve_port_open
if "%KEY_INDEX%"=="2" goto :resolve_port_kill
exit /b 1
:resolve_port_open
if defined DRY_RUN (
	echo   %C_MUTED%would open %~2%C_OFF%
) else (
	start "" "%~2"
)
exit /b 1
:resolve_port_kill
call :kill_pid %OWNER_PID% %~1
exit /b %errorlevel%

:kill_pid
echo.
echo   %C_DIM%Stopping PID %~1%C_OFF%
if defined DRY_RUN exit /b 0
taskkill /PID %~1 /T /F >nul 2>&1
rem Wait for the socket to close so the new process can bind the port.
for /l %%i in (1,1,15) do (
	call :port_owner %~2
	if errorlevel 1 exit /b 0
	"%SystemRoot%\System32\PING.EXE" -n 2 127.0.0.1 >nul
)
echo   %C_RED%Port %~2 is still in use.%C_OFF%
exit /b 1

:ask_browser
set "WANT_BROWSER=1"
if defined NO_BROWSER (
	set "WANT_BROWSER="
	exit /b 0
)
if defined ONESHOT exit /b 0
<nul set /p "=%C_MUTED%  open the browser when it is ready?%C_OFF% %C_RED%[Y/n]%C_OFF% "
choice /c yn /n /t 10 /d y 2>nul
if errorlevel 2 set "WANT_BROWSER="
exit /b 0

rem Opens the browser once the app answers, instead of guessing with a timer.
rem The waiter runs in its own minimized window so Ctrl+C here cannot reach it.
:open_when_ready
if defined DRY_RUN (
	echo   %C_MUTED%would open %~1 once it responds%C_OFF%
	exit /b 0
)
start "Moka.Red - waiting for %~1" /min cmd /c ""%SELF%" __wait_open "%~1" 240"
exit /b 0

:collect_tests
set "TEST_COUNT=0"
for /d %%d in ("%ROOT%\tests\*.Tests") do (
	set /a "TEST_COUNT+=1"
	set "TEST_!TEST_COUNT!=%%~nxd"
)
exit /b 0

:verify_wasm_guard
set "CORE_PKG=%PACK_OUT%\Moka.Red.Core.%VERSION%.nupkg"
if not exist "%CORE_PKG%" (
	echo.
	echo   %C_WARN%Could not find the Core package to verify the WebAssembly guard.%C_OFF%
	exit /b 0
)
rem A frameworkReference to Microsoft.AspNetCore.App in the nuspec breaks every
rem Blazor WebAssembly consumer at publish time with NETSDK1082.
powershell -NoProfile -Command "try { Add-Type -AssemblyName System.IO.Compression.FileSystem; $z = [IO.Compression.ZipFile]::OpenRead($env:CORE_PKG); $e = $z.Entries | Where-Object { $_.Name -like '*.nuspec' } | Select-Object -First 1; $r = New-Object IO.StreamReader($e.Open()); $t = $r.ReadToEnd(); $r.Dispose(); $z.Dispose(); if ($t -match 'frameworkReference') { exit 1 } else { exit 0 } } catch { exit 2 }" >nul 2>&1
set "GUARD_RC=%errorlevel%"
echo.
if "%GUARD_RC%"=="0" (
	echo   %C_GREEN%WebAssembly guard ok%C_OFF%   %C_DIM%no frameworkReference in the Core nuspec%C_OFF%
	exit /b 0
)
if "%GUARD_RC%"=="2" (
	echo   %C_WARN%Could not read the Core package to verify the WebAssembly guard.%C_OFF%
	exit /b 0
)
echo   %C_RED%WebAssembly guard FAILED%C_OFF%   the Core nuspec declares a frameworkReference.
echo   Blazor WebAssembly consumers will fail to publish with NETSDK1082.
echo   See StripAspNetCoreAppFrameworkRef in Directory.Build.targets.
set "LAST_RC=1"
exit /b 1

:docs_detect
set "DOCS_VER="
for /f "tokens=1,2" %%a in ('dotnet tool list --global 2^>nul') do if /i "%%a"=="mokadocs" set "DOCS_VER=%%b"
set "DOCS_HAS_SRC="
if exist "%DOCS_SRC%\Moka.Docs.Cli.csproj" set "DOCS_HAS_SRC=1"
if not defined DOCS_HAS_SRC set "DOCS_USE_SRC="
if defined DOCS_LATEST_CHECKED exit /b 0
set "DOCS_LATEST_CHECKED=1"
set "DOCS_LATEST="
for /f "delims=" %%v in ('powershell -NoProfile -Command "try { ((Invoke-RestMethod -TimeoutSec 5 'https://api.nuget.org/v3-flatcontainer/mokadocs/index.json').versions | Where-Object { $_ -notlike '*-*' })[-1] } catch { }" 2^>nul') do set "DOCS_LATEST=%%v"
exit /b 0

:docs_status_line
set "DS_TOOL=%C_WARN%not installed%C_OFF%"
if defined DOCS_VER set "DS_TOOL=%DOCS_VER%"
set "DS_NOTE="
if not defined DOCS_LATEST goto :docs_status_mode
if not defined DOCS_VER (
	set "DS_NOTE=%C_DIM%latest %DOCS_LATEST% on NuGet%C_OFF%"
	goto :docs_status_mode
)
call :semver_lt "%DOCS_VER%" "%DOCS_LATEST%"
if errorlevel 1 (
	set "DS_NOTE=%C_GREEN%up to date%C_OFF%"
) else (
	set "DS_NOTE=%C_WARN%update available: %DOCS_LATEST%%C_OFF%"
)
:docs_status_mode
set "DS_MODE=global tool"
if defined DOCS_USE_SRC set "DS_MODE=%C_WARN%local source%C_OFF%"
echo   %C_BOLD%DOCS%C_OFF%  %C_MUTED%mokadocs%C_OFF% !DS_TOOL!   !DS_NOTE!   %C_MUTED%running from%C_OFF% !DS_MODE!
exit /b 0

:docs_exe
set "DOCS_EXE="
if defined DOCS_USE_SRC (
	set "DOCS_EXE=dotnet run --project "!DOCS_SRC!" -f net10.0 --"
	exit /b 0
)
if defined DOCS_VER goto :docs_exe_tool
echo.
echo   %C_RED%mokadocs is not installed.%C_OFF%   Press U in the docs menu, or run: RunSamples.bat docs update
exit /b 1
:docs_exe_tool
set "DOCS_EXE=mokadocs"
where mokadocs >nul 2>&1
if errorlevel 1 set "DOCS_EXE="!USERPROFILE!\.dotnet\tools\mokadocs.exe""
exit /b 0

rem Exit code 0 when version %1 is older than %2. Pre-release suffixes are ignored.
:semver_lt
set "SA1=0" & set "SA2=0" & set "SA3=0" & set "SB1=0" & set "SB2=0" & set "SB3=0"
for /f "tokens=1-3 delims=.-+" %%a in ("%~1") do set /a "SA1=%%a+0, SA2=%%b+0, SA3=%%c+0"
for /f "tokens=1-3 delims=.-+" %%a in ("%~2") do set /a "SB1=%%a+0, SB2=%%b+0, SB3=%%c+0"
if %SA1% lss %SB1% exit /b 0
if %SA1% gtr %SB1% exit /b 1
if %SA2% lss %SB2% exit /b 0
if %SA2% gtr %SB2% exit /b 1
if %SA3% lss %SB3% exit /b 0
exit /b 1


rem ===========================================================================
rem  Browser waiter. Relaunched through open_when_ready, never called directly.
rem ===========================================================================

:wait_open
setlocal
set "WAIT_URL=%~2"
set "WAIT_SECONDS=%~3"
if not defined WAIT_SECONDS set "WAIT_SECONDS=240"
where curl.exe >nul 2>&1
if errorlevel 1 goto :wait_open_netstat
rem curl does the waiting: a refused connection counts as retryable and the total
rem wait is capped in seconds. Counting loop iterations instead drifts badly,
rem because each probe of a closed localhost port costs about two seconds on Windows.
curl.exe -s -k -o nul -m 5 --retry 100000 --retry-connrefused --retry-delay 1 --retry-max-time %WAIT_SECONDS% "%WAIT_URL%" >nul 2>&1
if not errorlevel 1 start "" "%WAIT_URL%"
exit

:wait_open_netstat
for /f "tokens=3 delims=:/" %%p in ("%WAIT_URL%") do set "WAIT_PORT=%%p"
set /a "WAIT_N=0"
:wait_open_loop
set /a "WAIT_N+=1"
if %WAIT_N% gtr %WAIT_SECONDS% exit
netstat -ano | findstr /r /c:":%WAIT_PORT% .*LISTENING" >nul
if not errorlevel 1 (
	start "" "%WAIT_URL%"
	exit
)
"%SystemRoot%\System32\PING.EXE" -n 2 127.0.0.1 >nul
goto :wait_open_loop
