@echo off
setlocal DisableDelayedExpansion

set "ARCHITECTURE=%~1"
if "%ARCHITECTURE%"=="" set "ARCHITECTURE=win-x64"
:: Shift the arguments to exclude the first one
shift
set "ARG2=%~1"
shift
set "PASSWORD=%~1"
shift

if "%ARG2%" == "" (
    ..\..\%ARCHITECTURE%\CloudPillar.Agent.exe
) else if not "%ARG2%" == "--winsrv" (
    %ARG2%\..\..\%ARCHITECTURE%\CloudPillar.Agent.exe %ARG2%
) else (
    ..\..\%ARCHITECTURE%\CloudPillar.Agent.exe %ARG2% "%PASSWORD%"
)
endlocal