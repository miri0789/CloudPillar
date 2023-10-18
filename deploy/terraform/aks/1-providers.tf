terraform {
  backend "azurerm" {
    subscription_id      = ""
    resource_group_name  = "cp-tf-rg"
    storage_account_name = "tfcp"
    container_name       = "tfstate"
    key                  = "aks.tfstate"
  }
  required_version = ">=0.12"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~>3.59.0"
    }
  }
}

provider "azurerm" {
  features {}
  subscription_id = ""
}