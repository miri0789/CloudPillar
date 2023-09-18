# Creating Azure Keyvault in the Resource Group
$keyVault = "$Env:TF_BACKEND_KV-$EnvName"

# Creating a secret in the keyvault and storing the service principal client id and secret
az keyvault secret set --vault-name $keyVault --name CLIENT-ID --value $Env:ARM_CLIENT_ID --output none
Write-Host "`n`n Keyvault Secret: CLIENT-ID Created `n"

az keyvault secret set --vault-name $keyVault --name CLIENT-SECRET --value $Env:ARM_CLIENT_SECRET --output none
Write-Host "`n`n Keyvault Secret: CLIENT-SECRET Created `n"

az keyvault secret set --vault-name $keyVault --name PAT --value $Env:PAT_VALUE --output none
Write-Host "`n`n Keyvault Secret: PAT Created `n"