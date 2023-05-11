resource "azurerm_virtual_network" "aks" {
  name                = "iot-${var.env}-vnet"
  resource_group_name = azurerm_resource_group.aks.name
  location            = azurerm_resource_group.aks.location
  tags                = { "terraform": "true" }
  address_space       = ["22.0.0.0/8"]
}

resource "azurerm_subnet" "aks" {
  name                 = "aks-subnet"
  virtual_network_name = azurerm_virtual_network.aks.name
  resource_group_name  = azurerm_resource_group.aks.name
  address_prefixes     = ["22.240.0.0/16"]
}
