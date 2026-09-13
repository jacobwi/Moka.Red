@echo off
echo Moka.Red - Clear build cache
echo.
powershell -ExecutionPolicy Bypass -File "%~dp0ClearProjectsCache.ps1" %*
pause