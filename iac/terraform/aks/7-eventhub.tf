
resource "azurerm_eventhub_namespace" "iot" {
  name                = "cp-${var.env}-eh-namespace"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  sku                 = "Basic"
}

resource "azurerm_eventhub" "iot" {
  name                = "cp-${var.env}-eh"
  resource_group_name = azurerm_resource_group.rg.name
  namespace_name      = azurerm_eventhub_namespace.iot.name
  partition_count     = 2
  message_retention   = 1
}

resource "azurerm_eventhub_authorization_rule" "iot" {
  resource_group_name = azurerm_resource_group.rg.name
  namespace_name      = azurerm_eventhub_namespace.iot.name
  eventhub_name       = azurerm_eventhub.iot.name
  name                = "acctest"
  send                = true
}
