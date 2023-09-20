
resource "azurerm_virtual_network" "aks" {
  name                 = "cp-ms-${var.env}-vnet"
  resource_group_name  = azurerm_resource_group.aks.name
  location             = azurerm_resource_group.aks.location
  tags                 = { "Terraform" : "true" }
  address_space        = ["11.0.0.0/8"]
}

resource "azurerm_subnet" "aks" {
  name                 = "aks-subnet"
  virtual_network_name = azurerm_virtual_network.aks.name
  resource_group_name  = azurerm_resource_group.aks.name
  address_prefixes     = ["11.240.0.0/16"]
}

resource "azurerm_subnet" "cp-keyvault-private-subnet" {
  name                 = "cp-keyvault-private-subnet"
  virtual_network_name = azurerm_virtual_network.aks.name
  resource_group_name  = azurerm_resource_group.aks.name
  address_prefixes     = ["11.2.10.0/24"]
  service_endpoints = [ "Microsoft.KeyVault" ,"Microsoft.Web" ,"Microsoft.ContainerRegistry"]
}
