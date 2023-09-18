# Creating Azure Keyvault in the Resource Group
$resourceGroup = "$Env:TF_BACKEND_RG-$EnvName"
$keyVault = "$Env:TF_BACKEND_KV-$EnvName"

az keyvault create --name $keyVault --resource-group $resourceGroup --location $Env:REGION --output none
Write-Host "`n`n Keyvault: $keyVault Created `n"

