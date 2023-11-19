resource "azurerm_storage_account" "iot" {
  # name has to be unique amongst all storage accounts in Azure could over the world
  name                     = "cpiot${var.env}files"
  resource_group_name      = azurerm_resource_group.iot.name
  location                 = azurerm_resource_group.iot.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_storage_container" "iot" {
  name                  = "iotcontainer"
  storage_account_name  = azurerm_storage_account.iot.name
  container_access_type = "private"
}

resource "azurerm_storage_container" "diagnostics" {
  name                  = "diagnostics-container"
  storage_account_name  = azurerm_storage_account.iot.name
  container_access_type = "private"
}


resource "azurerm_storage_management_policy" "diagnostics-policy" {
  storage_account_id = azurerm_storage_account.iot.id

  rule {
    name    = "delete-after-one-day"
    enabled = true
    filters {
      prefix_match = [azurerm_storage_container.diagnostics.name]
      blob_types   = ["blockBlob"]
    }
    actions {
      base_blob {
        delete_after_days_since_creation_greater_than = 1
      }
    }
  }
}
