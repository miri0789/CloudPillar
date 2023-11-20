resource "azurerm_resource_group" "aks" {
  name     = "cp-ms-${var.env}-rg"
  location = var.location
  tags = {"Terraform": "true"}
}