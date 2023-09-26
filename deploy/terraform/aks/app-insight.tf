resource "azurerm_application_insights" "aks" {
  name                = "cp-${var.env}-ai"
  resource_group_name = azurerm_resource_group.aks.name
  location            = var.location
  application_type    = "web"
}