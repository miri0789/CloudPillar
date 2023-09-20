resource "azurerm_storage_account" "aks" {
  name                      = "cpaks${var.env}storage"
  resource_group_name       = azurerm_resource_group.aks.name
  location                  = azurerm_resource_group.aks.location
  account_tier              = "Standard"
  account_replication_type  = "LRS"
  account_kind              = "StorageV2"
  enable_https_traffic_only = true
  allow_nested_items_to_be_public = false
}

resource "azurerm_storage_container" "aks" {
  name                  = "servicebusmessagingcontainer"
  storage_account_name  = azurerm_storage_account.aks.name
  container_access_type = "private"
}

resource "azurerm_key_vault_secret" "secret" {
  depends_on = [
    azurerm_key_vault.infr
  ]
  name         = "StorageBEConnectionStringKeyUrl"
  value        = azurerm_storage_account.aks.primary_connection_string
  key_vault_id = azurerm_key_vault.infr.id
}