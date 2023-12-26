# Publish self-contained deployment for all platforms and architectures

# Define the target platforms and architectures
$platforms = @(
    # "linux-x64",
    # "linux-arm",
    # "linux-arm64",
    # "osx-x64",
    # "osx-arm64",
    # "win-arm64",
    # "win-x86",
    "win-x64" 
)

# Get the full path of the script directory
$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path -Parent $scriptPath

# Define the source directory and zip file path
$sourceDir = Join-Path -Path $scriptDir -ChildPath "publish"
$zipName = "cloudpillar.zip"
$zipPath = Join-Path -Path $scriptDir -ChildPath "$zipName"
$targetZipPath = Join-Path -Path $sourceDir -ChildPath "$zipName"

$environments = @('dev', 'prod')
$envDirPath = Join-Path -Path $sourceDir -ChildPath "env"
New-Item -ItemType Directory -Path $envDirPath -Force

foreach ($env in $environments) {
    $subDirPath = Join-Path -Path $envDirPath -ChildPath $env
    New-Item -ItemType Directory -Path $subDirPath -Force
    Write-Host "Created folder: $subDirPath"
}

$appSettingsFileName = "appsettings.json"
$appSettingsPath = Join-Path -Path $scriptDir -ChildPath $appSettingsFileName
$log4netFileName = "log4net.config"
$log4netPath = Join-Path -Path $scriptDir -ChildPath $log4netFileName
$startAgentBatFileName = "startagent.bat"
$startAgentBatPath = Join-Path -Path $scriptDir -ChildPath $startAgentBatFileName
$startAgentShellFileName = "startagent.sh"
$startAgentShellPath = Join-Path -Path $scriptDir -ChildPath $startAgentShellFileName
$pkiDirName = "pki"
$pkiPath = Join-Path -Path $scriptDir -ChildPath $pkiDirName

$appSettingsDestinationPathDev = Join-Path -Path $sourceDir -ChildPath env/dev/$appSettingsFileName
$appSettingsDestinationPathProd = Join-Path -Path $sourceDir -ChildPath env/prod/$appSettingsFileName
$log4netDestinationPathDev = Join-Path -Path $sourceDir -ChildPath env/dev/$log4netFileName
$log4netDestinationPathProd = Join-Path -Path $sourceDir -ChildPath env/prod/$log4netFileName
$startAgentBatDestinationPathDev = Join-Path -Path $sourceDir -ChildPath env/dev/$startAgentBatFileName
$startAgentBatDestinationPathProd = Join-Path -Path $sourceDir -ChildPath env/prod/$startAgentBatFileName
$startAgentShellDestinationPathDev = Join-Path -Path $sourceDir -ChildPath env/dev/$startAgentShellFileName
$startAgentShellDestinationPathProd = Join-Path -Path $sourceDir -ChildPath env/prod/$startAgentShellFileName
$pkiDestinationPathDev = Join-Path -Path $sourceDir -ChildPath env/dev
$pkiDestinationPathProd = Join-Path -Path $sourceDir -ChildPath env/prod

# Remove the existing zip file if it exists
if (Test-Path $zipPath) {
    Remove-Item $zipPath
}
if (Test-Path $targetZipPath) {
    Remove-Item $targetZipPath
}
# Create the source directory if it doesn't exist
if (-not (Test-Path $sourceDir)) {
    New-Item -ItemType Directory -Path $sourceDir | Out-Null
}

# Copy appSettings.json and log4net files
Copy-Item -Path $appSettingsPath -Destination $appSettingsDestinationPathProd -Force
Copy-Item -Path $log4netPath -Destination $log4netDestinationPathProd -Force
Copy-Item -Path $startAgentBatPath -Destination $startAgentBatDestinationPathProd -Force
Copy-Item -Path $startAgentShellPath -Destination $startAgentShellDestinationPathProd -Force
Copy-Item -Path $pkiPath -Destination $pkiDestinationPathProd -Recurse -Force
Copy-Item -Path $appSettingsPath -Destination $appSettingsDestinationPathDev -Force
Copy-Item -Path $log4netPath -Destination $log4netDestinationPathDev -Force
Copy-Item -Path $startAgentBatPath -Destination $startAgentBatDestinationPathDev -Force
Copy-Item -Path $startAgentShellPath -Destination $startAgentShellDestinationPathDev -Force
Copy-Item -Path $pkiPath -Destination $pkiDestinationPathDev -Recurse -Force
Write-Host "* Publishing in $sourceDir..."

# Loop through each platform and architecture
foreach ($platform in $platforms) {
    Write-Host "*** Publishing for $platform..."

    # Publish the self-contained deployment and specify the output directory
    $publishCommand = "dotnet publish -r $platform --self-contained true -p:PublishTrimmed=true -c Release -o `"$sourceDir/$platform`""
    Invoke-Expression $publishCommand -ErrorAction Stop
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: Failed to publish for $platform"
        exit 1
    }
}

Write-Host "Publishing completed. Zipping to $zipPath"


# Create a new zip file and add all files and subdirectories, excluding the script file
Add-Type -A 'System.IO.Compression.FileSystem'
[System.IO.Compression.ZipFile]::CreateFromDirectory($sourceDir, $zipPath, 'Optimal', $false)

Write-Host "Zip file created: $zipPath"

# Move the zip file to the jnjiotagent directory
Move-Item -Path $zipPath -Destination $targetZipPath -Force
Write-Host "Zip file relocated: $targetZipPath"
