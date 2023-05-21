terraform {
  backend "azurerm" {
    subscription_id      = "a147112f-bc59-4e9e-ac2f-5b4585e6542e"
    resource_group_name  = "tf-rg"
    storage_account_name = "tfiotdicom"
    container_name       = "tfstate"
    key                  = "aks.tfstate"
  }
  required_version = ">=0.13"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~>2.0"
    }
    tls = {
      source = "hashicorp/tls"
      version = "~>4.0"
    }
  }
}

provider "azurerm" {
  features {}
}

# provider "azurerm" {
#   alias = "prod"
#   features {}
#   subscription_id = "ccf06142-bee8-4220-8360-ddb8dcde8ec4"
# }