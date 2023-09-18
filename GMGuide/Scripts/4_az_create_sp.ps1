# # Create Service Principal with Owner Role for the Subscription
$spName = "$Env:SP_NAME-$EnvName"
Write-Host "`n`n Creating Service Principal $spName `n"
$SP = az ad sp create-for-rbac --name "$spName" --role="Owner" --scopes="/subscriptions/$Env:ARM_SUBSCRIPTION_ID" --sdk-auth

$SP_INFO = $SP | ConvertFrom-Json
Write-Host "`n`n Service Principal: $SP_INFO `n"

$CLIENT_ID = $SP_INFO.clientId
Write-Host "`n`n Client ID: $CLIENT_ID `n"

$CLIENT_SECRET = $SP_INFO.clientSecret
Write-Host "`n`n Client Secret: $CLIENT_SECRET `n"

# Set environment variables for CLIENT_ID and CLIENT_SECRET
[Environment]::SetEnvironmentVariable('ARM_CLIENT_ID', $CLIENT_ID, [System.EnvironmentVariableTarget]::Process)
[Environment]::SetEnvironmentVariable('ARM_CLIENT_SECRET', $CLIENT_SECRET, [System.EnvironmentVariableTarget]::Process)
Write-Host "`n`n Client ID --> ARM_CLIENT_ID --> $ARM_CLIENT_ID`n"
Write-Host "`n`n Client Secret --> ARM_CLIENT_SECRET --> $ARM_CLIENT_ID `n"

# Append CLIENT_ID and CLIENT_SECRET to the .env file
$envFilePath = "../../Envs/$EnvName.env"
$envFileContent = Get-Content -Path $envFilePath


Add-Content -Path $envFilePath -Value "ARM_CLIENT_ID=$CLIENT_ID"
Add-Content -Path $envFilePath -Value "ARM_CLIENT_SECRET=$CLIENT_SECRET"

# Read the existing content of the .env file into an array

$updatedEnvFileContent = Get-Content -Path $envFilePath
Write-Host "`nUpdated .env File Content:"
$updatedEnvFileContent | ForEach-Object { Write-Host $_ }
























# # Initialize flags to check if variables already exist
# $clientIdExists = $false
# $clientSecretExists = $false

# # Loop through each line in the .env file
# foreach ($line in $envFileContent) {
#     if ($line -match "^ARM_CLIENT_ID=") {
#         $clientIdExists = $true
#     }
#     if ($line -match "^ARM_CLIENT_SECRET=") {
#         $clientSecretExists = $true
#     }
# }

# # Update or append CLIENT_ID and CLIENT_SECRET in the .env file
# if ($clientIdExists) {
#     $envFileContent = $envFileContent -replace "^ARM_CLIENT_ID=.*", "ARM_CLIENT_ID=$CLIENT_ID"
# } else {
#     Add-Content -Path $envFilePath -Value "`nARM_CLIENT_ID=$CLIENT_ID"
# }

# if ($clientSecretExists) {
#     $envFileContent = $envFileContent -replace "^ARM_CLIENT_SECRET=.*", "ARM_CLIENT_SECRET=$CLIENT_SECRET"
# } else {
#     Add-Content -Path $envFilePath -Value "`nARM_CLIENT_SECRET=$CLIENT_SECRET"
# }

# # Write the updated content back to the .env file if any variable was updated
# if ($clientIdExists -or $clientSecretExists) {
#     Set-Content -Path $envFilePath -Value $envFileContent
# }

# Add-Content -Path $envFilePath -Value "ARM_CLIENT_ID=$CLIENT_ID"
# Add-Content -Path $envFilePath -Value "ARM_CLIENT_SECRET=$CLIENT_SECRET"

# Write-Host "`n`n Client ID and Client Secret are set for $Env:SP_NAME `n"

# # Create App Registration for the Service Principal
# # Create a new Azure AD Enterprise Application and get the App ID

# Write-Host "`n`n Creating App Registration for the Service Principal $Env:SP_NAME`n"
# $APP_ID = az ad app create --display-name $Env:SP_NAME --query appId -o tsv
# # Output the App ID (optional)
# Write-Host "Created App ID: $APP_ID"
# $CLIENT_SECRET = az ad app credential reset --id $APP_ID --query password -o tsv
# Write-Host "`n`n Created Client Secret: $CLIENT_SECRET `n"


# # # Assign a Owner role to the application
# az role assignment create --assignee $APP_ID --role "Owner" --scope "/subscriptions/$Env:ARM_SUBSCRIPTION_ID"
# Write-Host "`n`n Assigned Owner Role to the Application $Env:SP_NAME `n"


# # # Create new Client Secret for the App Registration
# # az ad app credential reset --id $Env:APP_ID

# # Get the Client ID and Client Secret
# CLIENT_ID=$(az ad app show --id $Env:APP_ID --query "appId" -o tsv)
# CLIENT_SECRET=$(az ad app credential list --id $Env:CLIENT_ID --query "[].value" -o tsv)
# Write-Host "`n`n Client ID: $Env:CLIENT_ID `n"
# Write-Host "`n`n Client Secret: $Env:CLIENT_SECRET `n"



# # Create Client ID and Client Secret for the Service Principal
# az ad sp credential reset --id $Env:SP_NAME

# # Get the Client ID and Client Secret
# ARM_CLIENT_ID=$(az ad sp show --display-name $Env:SP_NAME --query "[].appId" -o tsv)
# ARM_CLIENT_SECRET=$(az ad sp credential list --id $Env:ARM_CLIENT_ID --query "[].value" -o tsv)
# Write-Host "`n`n Client ID: $Env:ARM_CLIENT_ID `n"

# Write-Host "`n`n Client Secret: $Env:ARM_CLIENT_SECRET `n"







