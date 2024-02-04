data "azuread_client_config" "current" {}

data "azurerm_kubernetes_cluster" "devbe-aks" {
   name                = "cp-${var.env}-aks"
   resource_group_name = "cp-ms-${var.env}-rg"
}

resource "azurerm_key_vault" "cert-kv" {
  name                = "cp-${var.env}be-kv"
  resource_group_name = "cp-iot-${var.env}-rg"
  location            = "${var.location}"
  tenant_id           = data.azurerm_client_config.current.tenant_id
  sku_name            = "standard"

  dynamic access_policy {
    for_each = [data.azuread_client_config.current.object_id, data.azurerm_kubernetes_cluster.devbe-aks.kubelet_identity[0].object_id]
    content {
      tenant_id               = data.azuread_client_config.current.tenant_id
      object_id               = access_policy.value
      secret_permissions      = ["Get", "List"]
      certificate_permissions = ["Get", "List"]
      key_permissions         = ["Get", "List"]
    }
  }
}