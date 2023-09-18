# Create a resource group
$resourceGroup = "$Env:TF_BACKEND_RG-$EnvName"
az group create --name $resourceGroup --location $Env:REGION -o none

Write-Host "`n`n Resource Group: $resourceGroup Created `n"



