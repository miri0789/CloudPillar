resource "azurerm_resource_group" "appgw" {
  location = var.location
  name = "${terraform.workspace}-rg"
}