resource "azurerm_application_insights" "ai" {
  name                = "cp-${var.env}-ai"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  application_type    = "web"
  retention_in_days   = 30
}