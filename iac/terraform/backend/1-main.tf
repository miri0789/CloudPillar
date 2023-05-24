# Create a Resource Group for the Terraform State File
resource "azurerm_resource_group" "state" {
  name      = var.tf_backend_rg
  location  = "west europe"
  /* lifecycle { prevent_destroy = true } */
  tags      = { terraform = "true" }
}

# Create a Storage Account for the Terraform State File
resource "azurerm_storage_account" "state" {
  name                     = var.tf_backend_sa
  resource_group_name      = azurerm_resource_group.state.name
  location                 = azurerm_resource_group.state.location
  account_kind             = "StorageV2"
  account_tier             = "Standard"
  account_replication_type = "LRS"
  allow_blob_public_access = false
  /* lifecycle                { prevent_destroy = true } */
  tags                     = { terraform = "true" }
}

# Create a Storage Container for the Tf State Files
resource "azurerm_storage_container" "state" {
  name                  = var.tf_backend_container
  storage_account_name  = azurerm_storage_account.state.name
  container_access_type = "private"
}
