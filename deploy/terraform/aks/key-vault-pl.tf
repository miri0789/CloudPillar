
resource "azurerm_private_dns_zone" "aks" {
  name                = "privatelink.vaultcore.azure.net"
  resource_group_name = azurerm_resource_group.aks.name
  tags = {"Terraform": "true"}
}

resource "azurerm_private_dns_zone_virtual_network_link" "aks" {
  name                = "vnet-link"
  resource_group_name = azurerm_resource_group.aks.name
  private_dns_zone_name = azurerm_private_dns_zone.aks.name
  virtual_network_id = azurerm_virtual_network.aks.id
}


locals {
  key-vault = {
        infr = azurerm_key_vault.infr.id
  }
}

resource "azurerm_private_endpoint" "aks" {
  depends_on = [
    azurerm_key_vault.infr
  ]
  for_each            = local.key-vault
  name                = "${each.key}-kv-aks-${var.env}-pl"
  location            = azurerm_resource_group.aks.location
  tags                = { "Terraform" : "true" }
  resource_group_name = azurerm_resource_group.aks.name
  subnet_id           = azurerm_subnet.cp-keyvault-private-subnet.id

  private_dns_zone_group {
    name                 = "ZoneGroup"
    private_dns_zone_ids = [azurerm_private_dns_zone.aks.id]
  }

  private_service_connection {
    name                           = "kv-aks-${var.env}-pl"
    is_manual_connection           = false
    private_connection_resource_id = each.value
    subresource_names              = ["vault"]
  }
}

resource "azurerm_role_assignment" "kv" {
  depends_on = [
    azurerm_key_vault.infr
  ]
  principal_id                     =  azurerm_kubernetes_cluster.aks.kubelet_identity[0].object_id
  role_definition_name             = "Key Vault Secrets User"
  scope                            = azurerm_key_vault.infr.id
  skip_service_principal_aad_check = true
}