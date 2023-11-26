# Create a Resource Group for the Terraform State File
resource "azurerm_resource_group" "state" {
  name     = "cp-tf-rg"
  location = var.location
  tags     = { Terraform = "true" }
  lifecycle  { prevent_destroy = true }
}

# Create a Storage Account for the Terraform State File
resource "azurerm_storage_account" "state" {
  name                      = "tfcp"
  resource_group_name       = azurerm_resource_group.state.name
  location                  = var.location
  account_kind              = "StorageV2"
  account_tier              = "Standard"
  account_replication_type  = "LRS"
  allow_blob_public_access  = false
  enable_https_traffic_only = true
  tags                      = { Terraform = "true" }
  lifecycle                   { prevent_destroy = true }
}

# Create a Storage Container for the Tf State Files
resource "azurerm_storage_container" "state" {
  name                  = "tfstate"
  storage_account_name  = azurerm_storage_account.state.name
  container_access_type = "private"
}
