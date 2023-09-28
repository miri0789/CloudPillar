resource "tls_private_key" "kv" {
    algorithm = "RSA"
    rsa_bits = 4096
}

data "azurerm_client_config" "current" {
}

resource "azurerm_key_vault" "infr" { 
    name = "cp-${var.env}-details-kv"
    location = var.location
    resource_group_name = azurerm_resource_group.aks.name
    tenant_id = data.azurerm_client_config.current.tenant_id
    
    sku_name = "standard"
    
    enabled_for_deployment = true
    enabled_for_disk_encryption = true
    enabled_for_template_deployment = true
    purge_protection_enabled    = false

    access_policy {
        tenant_id = data.azurerm_client_config.current.tenant_id
        object_id = data.azurerm_client_config.current.object_id
        secret_permissions = [
            "Get",
            "List",
            "Set",
            "Delete",
            "Backup",
            "Recover",
            "Restore"
        ]
        key_permissions = [
            "Get",
            "List",
            "Sign",
            "Delete",
            "Backup",
            "Recover",
            "Restore",
            "Encrypt",
            "Decrypt",
            "UnwrapKey",
            "WrapKey",
            "Verify"
        ]
        storage_permissions = [
            "Get",
            "List",
            "Delete",
            "Set",
            "Update"
        ]
    }

}

resource "azurerm_key_vault_secret" "kv_pat" {
  name         = "AksPAT"
  value        = var.akspat
  key_vault_id = azurerm_key_vault.infr.id
}