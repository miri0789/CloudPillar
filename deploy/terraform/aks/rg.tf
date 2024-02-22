resource "azurerm_resource_group" "aks" {
  name     = "cp-ms-${var.env}-rg"
  location = var.location
  tags = {"Terraform": "true"}
}

resource "azurerm_resource_group" "int-cyber" {
  name     = "cp-int-cyber-${var.env}-rg"
  location = var.location
  tags     = {"Terraform": "true"}
}
