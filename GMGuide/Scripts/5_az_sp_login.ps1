# AZ Login with service principal 
az login --service-principal --username $Env:ARM_CLIENT_ID --password $Env:ARM_CLIENT_SECRET --tenant $Env:ARM_TENANT_ID --output none

Write-Host "`n`n Logged in with Service Principal `n"