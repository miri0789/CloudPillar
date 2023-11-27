@echo off
setlocal
set ARCHITECTURE=%~1

:: Shift the arguments to exclude the first one
shift

:: Run the self-contained deployment
%ARCHITECTURE%\CloudPillar.Agent.exe %*


rem cmd /C "set DEVICE_CONNECTION_STRING=%~1&& dotnet ./scbin/jnjiotagent.dll %*"