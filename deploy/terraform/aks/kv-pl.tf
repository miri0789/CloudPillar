data "azurerm_private_dns_zone" "kv" {
  name                = "privatelink.vaultcore.azure.net"
  resource_group_name = "cp-ms-${var.env}-rg"
}

resource "azurerm_private_endpoint" "kv" {
  name                = "cp-cert-${var.env}be-kv-pl"
  location            = "${var.location}"
  resource_group_name = "cp-ms-${var.env}-rg"
  subnet_id           = azurerm_subnet.aks.id

  private_dns_zone_group {
    name                 = "ZoneGroup"
    private_dns_zone_ids = [data.azurerm_private_dns_zone.kv.id]
  }

  private_service_connection {
    name                           = "kv-private-service-conn"
    is_manual_connection           = false
    private_connection_resource_id = azurerm_key_vault.cert-kv.id
    subresource_names              = ["vault"]
  }
}

