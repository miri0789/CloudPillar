data "azurerm_key_vault" "appgw" {
  name                = "cp-${terraform.workspace}be-kv"
  resource_group_name = "cp-ms-${terraform.workspace}-rg"
}

data "azurerm_app_service_certificate_order" "appgw" {
  name                = "${terraform.workspace}be-cloudpillar-net"
  resource_group_name = "cp-ms-${terraform.workspace}-rg"
}

data "azurerm_key_vault_secret" "appgw" {
  name         = data.azurerm_app_service_certificate_order.appgw.key_vault_secret_name
  key_vault_id = data.azurerm_key_vault.appgw.id
}




# data "azurerm_key_vault_certificate" "appgw" {
#   name         = "DomainCert"
#   key_vault_secret_id  = data.azurerm_key_vault.appgw.id
# }

# data "azurerm_app_service_certificate_order" "appgw" {
#   name                = "${terraform.workspace}be-cloudpillar-net"
#   resource_group_name = "cp-iot-${terraform.workspace}-rg"
# }

resource "azurerm_user_assigned_identity" "appgw" {
  name                = "cp-appgw-${terraform.workspace}-identity"
  resource_group_name = azurerm_resource_group.appgw.name
  location            = azurerm_resource_group.appgw.location
}

resource "azurerm_key_vault_access_policy" "appgw" {
  key_vault_id = data.azurerm_key_vault.appgw.id
  tenant_id    = azurerm_user_assigned_identity.appgw.tenant_id
  object_id    = azurerm_user_assigned_identity.appgw.principal_id
  certificate_permissions = [ "Get", "List" ]
  secret_permissions      = [ "Get", "List" ]
}