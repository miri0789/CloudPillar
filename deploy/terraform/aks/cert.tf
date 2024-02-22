resource "azurerm_app_service_certificate_order" "app-cert" {
  name                = "${var.env}be-cloudpillar-net"
  resource_group_name = "cp-ms-${var.env}-rg"
  location            = "global"
  distinguished_name  = "CN=${var.env}be.cloudpillar.net"
  product_type        = "Standard"
  auto_renew = true
}

resource "azurerm_app_service_certificate_order" "app-cert-int" {
  name                = "${var.env}be-cp-int-net"
  resource_group_name = azurerm_resource_group.int-cyber.name
  location            = "global"
  distinguished_name  = "CN=be.cp-int-${var.env}.net"
  product_type        = "Standard"
  auto_renew = true
}