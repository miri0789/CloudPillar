# Define script parameters
param (
    [string]$devices,
    [string]$blobName,
    [string]$destinationPath,
    [string]$filePath = $null
)

# Read Azure credentials and SAS token
$credFilePath = "./creds/azureAuth.json"
$sasTokenFilePath = "./creds/sasToken.txt"
$creds = Get-Content -Path $credFilePath | ConvertFrom-Json
$sasToken = Get-Content -Path $sasTokenFilePath

$SecurePassword = ConvertTo-SecureString -String $creds.password -AsPlainText -Force
$Credential = New-Object -TypeName System.Management.Automation.PSCredential -ArgumentList $creds.appId, $SecurePassword

Connect-AzAccount -ServicePrincipal -TenantId $creds.tenantId -Credential $Credential

$wasZipped = $false
# Check and potentially zip the directory
if (![String]::IsNullOrWhiteSpace($filePath) -and (Test-Path -Path $filePath -PathType Container)) {
    $zipPath = "$filePath.zip"
    Compress-Archive -Path $filePath -DestinationPath $zipPath -Force
    $filePath = $zipPath
    # Update blobName to include the zip file name if a directory is zipped
    $blobName = [System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName($blobName), [System.IO.Path]::GetFileName($filePath))
    $wasZipped = $true
}

# Upload file to Azure Blob Storage using SAS Token, if filePath is provided
if (![String]::IsNullOrWhiteSpace($filePath)) {
    $storageAccountName = "cpiottstfiles"
    $containerName = "iotcontainer"
    $sasToken = if ($sasToken.StartsWith('?')) { $sasToken } else { "?$sasToken" }
    $blobEndpointUri = "https://$storageAccountName.blob.core.windows.net/$containerName/$blobName$sasToken"
    
    Invoke-RestMethod -Method Put -Uri $blobEndpointUri -Headers @{"x-ms-blob-type"="BlockBlob"} -InFile $filePath
}


# API call with devices parameter
$apiUrl = "https://tstbe.cloudpillar.net/beapi-service/ChangeSpec/AssignChangeSpec?devices=$devices&changeSpecKey=changeSpec"

$headers = @{
    "accept" = "*/*"
    "Content-Type" = "application/json"
}
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$body = @{
    id = "ChangeSpecId_$timestamp"
    patch = @{
        transitPackage = @(
            @{
                source = "$blobName"
                destinationPath = "$destinationPath"
                unzip = $wasZipped
                action = "SingularDownload"
                description = "deny"
            }
        )
    }
} | ConvertTo-Json -Depth 3

Invoke-RestMethod -Method Post -Uri $apiUrl -Headers $headers -Body $body