# resource "azurerm_app_configuration" "config" {
#   name                = "cp-${var.env}-config"
#   resource_group_name = azurerm_resource_group.rg.name
#   location            = azurerm_resource_group.rg.location
# }