// In future there should be established also a white-acr
resource "azurerm_container_registry" "acr"{
  count                         = var.env == "dev" || var.env == "tst" ? 1 : 0 
  name                          = "cp${var.env}acr"
  resource_group_name           = azurerm_resource_group.rg.name
  location                      = azurerm_resource_group.rg.location
  sku                           = "Standard"
  admin_enabled                 = false
  public_network_access_enabled = true
  network_rule_bypass_option    = "AzureServices"
  zone_redundancy_enabled       = false
  anonymous_pull_enabled        = false
}