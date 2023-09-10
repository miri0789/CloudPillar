resource "azurerm_iothub" "iot" {
  name                = "cp-${var.env}-iothub"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location

  sku {
    name     = "S1"
    capacity = "2"
  }  

  cloud_to_device {
    max_delivery_count = 30
    default_ttl        = "PT1H"
    feedback {
      time_to_live       = "PT1H10M"
      max_delivery_count = 15
      lock_duration      = "PT30S"
    }
  }

  tags = {
    purpose = "testing"
  }

  
}

resource "azurerm_iothub_route" "device_twin_changes" {
  name          = "DeviceUpdate.DeviceTwinChanges"
  resource_group_name = azurerm_resource_group.rg.name
  iothub_name   = azurerm_iothub.iot.name
  source        = "TwinChangeEvents"
  condition     = "(opType = 'updateTwin' OR opType = 'replaceTwin') AND IS_DEFINED($body.tags.ADUGroup)"
  endpoint_names = ["events"]
  enabled        = true
}

resource "azurerm_iothub_route" "device_digital_twin_changes" {
  name          = "DeviceUpdate.DigitalTwinChanges"
  resource_group_name = azurerm_resource_group.rg.name
  iothub_name   = azurerm_iothub.iot.name
  source        = "DigitalTwinChangeEvents"
  condition     = "true"
  endpoint_names = ["events"]
  enabled        = true
} 

resource "azurerm_iothub_route" "device_lifecycle" {
  name          = "DeviceUpdate.DeviceLifeCycle"
  resource_group_name = azurerm_resource_group.rg.name
  iothub_name   = azurerm_iothub.iot.name
  source        = "DeviceLifecycleEvents"
  condition     = "opType = 'deleteDeviceIdentity' OR opType = 'deleteModuleIdentity'"
  endpoint_names = ["events"]
  enabled       = true
}
resource "azurerm_iothub_file_upload" "iot" {
  iothub_id         = azurerm_iothub.iot.id
  connection_string = azurerm_storage_account.iot.primary_blob_connection_string
  container_name    = azurerm_storage_container.iot.name
}