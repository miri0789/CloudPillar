@echo off
setlocal
set ARCHITECTURE=%~1
set DEVICE_CONNECTION_STRING=%~2

if "%DEVICE_CONNECTION_STRING%"=="" (
    echo Usage: startagent.bat ARCHITECTURE DEVICE_CONNECTION_STRING 
    exit /b 1
)

:: Shift the arguments to exclude the first one
shift

:: Run the self-contained deployment
%ARCHITECTURE%\jnjiotagent.exe %*


rem cmd /C "set DEVICE_CONNECTION_STRING=%~1&& dotnet ./scbin/jnjiotagent.dll %*"