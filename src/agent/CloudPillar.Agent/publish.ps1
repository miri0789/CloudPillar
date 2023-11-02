# Publish self-contained deployment for all platforms and architectures

# Define the target platforms and architectures
$platforms = @(
    "linux-x64",
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
$appSettingsFileName = "appsettings.json"
$appSettingsPath = Join-Path -Path $scriptDir -ChildPath $appSettingsFileName
$appSettingsDestinationPath = Join-Path -Path $sourceDir -ChildPath $appSettingsFileName
$log4netFileName = "log4net.config"
$log4netPath = Join-Path -Path $scriptDir -ChildPath $log4netFileName
$log4netDestinationPath = Join-Path -Path $sourceDir -ChildPath $log4netFileName

# Remove the existing zip file if it exists
if (Test-Path $zipPath) {
    Remove-Item $zipPath
}
if (Test-Path $targetZipPath) {
    Remove-Item $targetZipPath
}
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

# Create the source directory if it doesn't exist
if (-not (Test-Path $sourceDir)) {
    New-Item -ItemType Directory -Path $sourceDir | Out-Null
}

# Copy appSettings.json and log4net files
Copy-Item -Path $appSettingsPath -Destination $appSettingsDestinationPath -Force
Copy-Item -Path $log4netPath -Destination $log4netDestinationPath -Force
pro
# Create a new zip file and add all files and subdirectories, excluding the script file
Add-Type -A 'System.IO.Compression.FileSystem'
[System.IO.Compression.ZipFile]::CreateFromDirectory($sourceDir, $zipPath, 'Optimal', $false)

Write-Host "Zip file created: $zipPath"

# Move the zip file to the jnjiotagent directory
Move-Item -Path $zipPath -Destination $targetZipPath -Force
Write-Host "Zip file relocated: $targetZipPath"
