resource "azurerm_virtual_network" "aks" {
  name                = "cp-${var.env}-vnet"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  tags                = { "terraform": "true" }
  address_space       = ["${var.ip-pfx}.0.0.0/8"]
}

resource "azurerm_subnet" "aks" {
  name                 = "aks-subnet"
  virtual_network_name = azurerm_virtual_network.aks.name
  resource_group_name  = azurerm_resource_group.rg.name
  address_prefixes     = ["${var.ip-pfx}.240.0.0/16"]
}
