resource "azurerm_public_ip" "appgw" {
  name                = "cp-appgw-${terraform.workspace}-ip"
  location            = azurerm_resource_group.appgw.location
  resource_group_name = azurerm_resource_group.appgw.name
  allocation_method   = "Static"
  sku                 = "Standard"
  domain_name_label   = "cartonet${terraform.workspace}appgw"
  zones               = [ "1" ]
}