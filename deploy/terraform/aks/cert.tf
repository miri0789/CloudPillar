resource "azurerm_app_service_certificate_order" "app-cert" {
  name                = "${var.env}be-cloudpillar-net"
  resource_group_name = "cp-ms-${var.env}-rg"
  location            = "global"
  distinguished_name  = "CN=${var.env}be.cloudpillar.net"
  product_type        = "Standard"
  auto_renew = true
}