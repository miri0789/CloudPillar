resource "azurerm_app_configuration" "aks" {
  name                = "cp-${var.env}-config"
  resource_group_name = azurerm_resource_group.aks.name
  location            = var.location
}