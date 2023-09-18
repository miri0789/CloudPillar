<#
    Execute this script to create the backend resources for Terraform
    .\tf_backend_creator.ps1 -EnvName <env> (dev, test, prod)
#>



param(
    [string]$EnvName = "dev"
)

# .\0_sourceEnv.ps1 -EnvName $EnvName
# .\1_azLogin.ps1
# .\2_azSetSub.ps1
# .\3_az_create_rg.ps1
# .\4_az_create_sp.ps1
.\0_sourceEnv.ps1 -EnvName $EnvName
.\5_az_sp_login.ps1
.\6_az_create_sa.ps1
.\7_az_create_keyvault.ps1
.\8_az_create_secrets.ps1

