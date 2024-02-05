data "azurerm_key_vault" "appgw" {
  name                = "cp-cert-${terraform.workspace}-kv"
  resource_group_name = "cp-int-cyber-${terraform.workspace}-rg"
}

data "azurerm_key_vault_certificate" "appgw" {
  name         = "DomainCert"
  key_vault_id = data.azurerm_key_vault.appgw.id
}

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