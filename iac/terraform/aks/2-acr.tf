// In future there should be established also a white-acr
resource "azurerm_container_registry" "acr"{
  count                         = var.env == "dev" ? 1 : 0
  name                          = "cpgreyacr"
  resource_group_name           = "cp-rg"
  location                      = azurerm_resource_group.aks.location
  sku                           = "Standard"
  admin_enabled                 = false
  public_network_access_enabled = true
  network_rule_bypass_option    = "AzureServices"
  zone_redundancy_enabled       = false
  anonymous_pull_enabled        = false
}

data "azurerm_container_registry" "aks"{
  #count                         = var.env == "dev" ? 1 : 0
  name                          = "cpgreyacr"
  resource_group_name           = "cp-rg"
  depends_on = [ azurerm_container_registry.acr ]
}
