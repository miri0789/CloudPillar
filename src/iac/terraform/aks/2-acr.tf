resource "azurerm_container_registry" "aks"{
  name                          = "iot-dicom-acr"
  resource_group_name           = azurerm_resource_group.aks.name
  location                      = azurerm_resource_group.aks.location
  sku                           = "Standard"
  admin_enabled                 = false
  public_network_access_enabled = true
  network_rule_bypass_option    = "AzureServices"
  zone_redundancy_enabled       = false
  anonymous_pull_enabled        = false
}