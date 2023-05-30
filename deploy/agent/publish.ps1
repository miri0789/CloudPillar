$scriptDir = Join-Path -Path $PSScriptRoot -ChildPath "../../src/agent/dotnet"
pushd $scriptDir
Invoke-Expression -Command "& './publish.ps1'"
popd