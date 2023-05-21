resource "azurerm_resource_group" "aks" {
  name     = "iot-${var.env}-rg"
  location  = "${var.location}"
}

resource "azurerm_log_analytics_workspace" "aks" {
  name                = "iot-${var.env}-log"
  location            = azurerm_resource_group.aks.location
  resource_group_name = azurerm_resource_group.aks.name
  sku                 = "Standalone" #"PerGB2018"
  retention_in_days   = 30
}