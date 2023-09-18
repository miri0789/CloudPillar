# Login to Azure with Device Code
az login --tenant $Env:ARM_TENANT_ID --use-device-code --output none
Write-Host "`n`n Logged in to Azure `n"


