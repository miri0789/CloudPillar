data "azurerm_resource_group" "iot" {
  name     = "iot-${var.env}-rg"
}

resource "azurerm_eventhub_namespace" "iot" {
  name                = "iot-${var.env}-eh-namespace"
  resource_group_name = data.azurerm_resource_group.iot.name
  location            = data.azurerm_resource_group.iot.location
  sku                 = "Basic"
}

resource "azurerm_eventhub" "iot" {
  name                = "iot-${var.env}-eh"
  resource_group_name = data.azurerm_resource_group.iot.name
  namespace_name      = azurerm_eventhub_namespace.iot.name
  partition_count     = 2
  message_retention   = 1
}

resource "azurerm_eventhub_authorization_rule" "iot" {
  resource_group_name = data.azurerm_resource_group.iot.name
  namespace_name      = azurerm_eventhub_namespace.iot.name
  eventhub_name       = azurerm_eventhub.iot.name
  name                = "acctest"
  send                = true
}
