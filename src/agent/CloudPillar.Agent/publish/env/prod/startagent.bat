@echo off
setlocal
set ARCHITECTURE=%~1
if "%ARCHITECTURE%"=="" set ARCHITECTURE=win-x64
:: Shift the arguments to exclude the first one
shift

:: Run the self-contained deployment
..\..\%ARCHITECTURE%\CloudPillar.Agent.exe %*

