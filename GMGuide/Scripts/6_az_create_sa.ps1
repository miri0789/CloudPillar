
# create storage account with container named tfstate
$saName="$Env:TF_BACKEND_SA$EnvName"
$resourceGroup = "$Env:TF_BACKEND_RG-$EnvName"

az storage account create --name $saName --resource-group $resourceGroup --location $Env:REGION --sku Standard_LRS -o none
Write-Host "`n`n Storage Account: $saName Created `n"

az storage container create --name tfstate --account-name $saName
Write-Host "`n`n Storage Account Container: tfstate Created `n"

