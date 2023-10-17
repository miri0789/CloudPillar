resource "azurerm_container_registry" "aks" {
  count               = var.env == "dev" || var.env == "tst" ? 1 : 0
  name                = "cp${var.env}acr"
  resource_group_name = azurerm_resource_group.aks.name
  location            = var.location
  tags                = {"Terraform": "true"}
  sku                 = "Standard"
  admin_enabled       = false
  public_network_access_enabled = true
  network_rule_bypass_option = "AzureServices"
  zone_redundancy_enabled = false
  anonymous_pull_enabled = false
}

resource "azurerm_role_assignment" "aks" {
  principal_id                     =  azurerm_kubernetes_cluster.aks.kubelet_identity[0].object_id
  role_definition_name             = "AcrPull"
  scope                            = azurerm_container_registry.aks[0].id
  skip_service_principal_aad_check = true
}