// IoT hub connection string Secret
/*data "azurerm_iothub" "aks" {
  # name has to be unique amongst all iot hubs in Azure could over the world
  name                = "cp-${var.env}-iothub"
  resource_group_name = azurerm_resource_group.rg.name
}
resource "kubernetes_secret" "iothub" {
  metadata {
    name = "cp-iothub-conn"
  }
  data = {
    connstr = data.azurerm_iothub.aks.connection_string
  }
}

// IoT hub Storage connection string Secret
data "azurerm_storage_account" "aks" {
  # name has to be unique amongst all storage accounts in Azure could over the world
  name                = "cp${var.env}storage"
  resource_group_name = azurerm_resource_group.rg.name
}
resource "kubernetes_secret" "storage" {
  metadata {
    name = "cp-storage-conn"
  }
  data = {
    connstr = data.azurerm_storage_account.aks.primary_blob_connection_string
  }
}*/
