data "azurerm_iothub_shared_access_policy" "iothub_iothubowner" {
    name                = "iothubowner"
    resource_group_name = azurerm_resource_group.iot.name
    iothub_name         = azurerm_iothub.iot.name
}


resource "azurerm_iothub_dps" "iot" {
  name                = "cp-${var.env}-dps"
  resource_group_name = azurerm_resource_group.iot.name
  location            = azurerm_resource_group.iot.location
  allocation_policy   = "Hashed"

  sku {
    name     = "S1"
    capacity = "1"
  }

  linked_hub {
    connection_string = data.azurerm_iothub_shared_access_policy.iothub_iothubowner.primary_connection_string
    location          = azurerm_iothub.iot.location
  }
}

resource "null_resource" "create-dps-enrollement" {
  depends_on = [
    azurerm_iothub_dps.iot
  ]
  provisioner "local-exec" {
    command = <<-EOT
      az iot dps enrollment-group create -g ${azurerm_resource_group.iot.name} --dps-name ${azurerm_iothub_dps.iot.name} --enrollment-id pre-shared-group
    EOT
  }
}