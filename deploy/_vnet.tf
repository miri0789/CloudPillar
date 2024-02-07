
resource "azurerm_virtual_network" "aks" {
  name                 = "cp-ms-${var.env}-vnet"
  resource_group_name  = azurerm_resource_group.aks.name
  location             = azurerm_resource_group.aks.location
  tags                 = { "Terraform" : "true" }
  address_space        = [var.addressSpace]
}

resource "azurerm_subnet" "aks" {
  name                 = "default"
  virtual_network_name = azurerm_virtual_network.aks.name
  resource_group_name  = azurerm_resource_group.aks.name
  address_prefixes     = [var.addressSpace]
}

resource "azurerm_role_assignment" "ra" {
  principal_id                     = azurerm_kubernetes_cluster.aks.identity[0].principal_id
  role_definition_name             = "Network Contributor"
  scope                            = azurerm_virtual_network.aks.id
  skip_service_principal_aad_check = true
}

data "azurerm_private_dns_zone" "aks" {
  count = "${var.env}"=="dev" || var.env == "tst" ? 0 : 1
  name  = replace(azurerm_kubernetes_cluster.aks.private_fqdn,"/${var.env}be.[^.]*\\./","")
}

resource "azurerm_private_dns_zone_virtual_network_link" "hub-link" {
  count                 = "${var.env}"=="dev" || var.env == "tst" ? 0 : 1
  name                  = "hub-link"
  virtual_network_id    = azurerm_virtual_network.aks.id
  resource_group_name   = data.azurerm_private_dns_zone.aks[0].resource_group_name
  private_dns_zone_name = data.azurerm_private_dns_zone.aks[0].name
}