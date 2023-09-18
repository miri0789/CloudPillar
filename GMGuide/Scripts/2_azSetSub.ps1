# Set the current subscription
az account set --subscription $Env:ARM_SUBSCRIPTION_ID
Write-Host "`n`n Subscription: $Env:ARM_SUBSCRIPTION_NAME `n"

