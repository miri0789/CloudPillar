@echo off
setlocal
set ARCHITECTURE=%~1
if "%ARCHITECTURE%"=="" set ARCHITECTURE=win-x64
:: Shift the arguments to exclude the first one
shift
set WINSEERV=%~1
shift
set WORKINGDIR=%~1
shift

:: Run the self-contained deployment

..\..\%ARCHITECTURE%\CloudPillar.Agent.exe %WORKINGDIR% %WINSEERV% %*

