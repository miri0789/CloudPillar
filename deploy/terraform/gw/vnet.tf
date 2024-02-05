resource "azurerm_virtual_network" "appgw" {
  name                 = "cp-appgw-${terraform.workspace}-vnet"
  resource_group_name  = azurerm_resource_group.appgw.name
  location             = azurerm_resource_group.appgw.location
  address_space        = [ "10.4.0.0/16" ]
}

resource "azurerm_subnet" "appgw" {
  name                 = "default"
  virtual_network_name = azurerm_virtual_network.appgw.name
  resource_group_name  = azurerm_resource_group.appgw.name
  address_prefixes     = ["10.4.0.0/24"]
  service_endpoints    = ["Microsoft.Web"]
}