# For env specific resources
resource "azurerm_resource_group" "aks" {
  name     = "iot-${var.env}-rg"
  location  = "${var.location}"
}

# For common env resources
resource "azurerm_resource_group" "acr" {
  count     = var.env == "dev" ? 1 : 0
  name      = "iot-rg"
  location  = "${var.location}"
}

resource "azurerm_log_analytics_workspace" "aks" {
  name                = "iot-${var.env}-log"
  location            = azurerm_resource_group.aks.location
  resource_group_name = azurerm_resource_group.aks.name
  sku                 = "PerGB2018" # "Standalone" #
  retention_in_days   = 30
}