resource "azurerm_app_configuration" "aks" {
  name                = "cp-${var.env}-conf"
  resource_group_name = azurerm_resource_group.aks.name
  location            = var.location
  sku                 = "free"
}


resource "azurerm_role_assignment" "appconf_dataowner" {
  scope                = azurerm_app_configuration.aks.id
  role_definition_name = "App Configuration Data Owner"
  principal_id         = data.azurerm_client_config.current.object_id
}

resource "azurerm_app_configuration_key" "key1" {
  depends_on = [
    azurerm_role_assignment.appconf_dataowner
  ]
  configuration_store_id = azurerm_app_configuration.aks.id
  key                    = "Logging:LogLevel:RefreshInterval"
  value                  = "15000"
}

resource "azurerm_app_configuration_key" "key2" {
  depends_on = [
    azurerm_role_assignment.appconf_dataowner
  ]
  configuration_store_id = azurerm_app_configuration.aks.id
  key                    = "Log4Net:LogLevel:Default"
  value                  = "INFO"
}

resource "azurerm_app_configuration_key" "key3" {
  depends_on = [
    azurerm_role_assignment.appconf_dataowner
  ]
  configuration_store_id = azurerm_app_configuration.aks.id
  key                    = "Log4Net:LogLevel:AppInsights"
  value                  = "INFO"
}

resource "azurerm_app_configuration_key" "key4" {
  depends_on = [
    azurerm_role_assignment.appconf_dataowner
  ]
  configuration_store_id = azurerm_app_configuration.aks.id
  key                    = "Log4Net:LogLevel:Appenders"
  value                  = "INFO"
}

resource "azurerm_app_configuration_key" "key5" {
  depends_on = [
    azurerm_application_insights.aks, azurerm_role_assignment.appconf_dataowner
  ]
  configuration_store_id = azurerm_app_configuration.aks.id
  key                    = "Logging:AppInsights:ConnectionString"
  value                  = azurerm_application_insights.aks.connection_string
}

resource "azurerm_app_configuration_key" "key6" {  
  depends_on = [
    azurerm_application_insights.aks, azurerm_role_assignment.appconf_dataowner
  ]
  configuration_store_id = azurerm_app_configuration.aks.id
  key                    = "Logging:AppInsights:InstrumentationKey"
  value                  = azurerm_application_insights.aks.instrumentation_key
}

