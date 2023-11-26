resource "azurerm_resource_group" "iot" {
  name     = "cp-iot-${var.env}-rg"
  location = var.location
  tags = {"Terraform": "true"}
}