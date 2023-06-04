terraform {
  backend "azurerm" {
    subscription_id      = "a147112f-bc59-4e9e-ac2f-5b4585e6542e"
    resource_group_name  = "tf-rg"
    storage_account_name = "tfcloudpillar"
    container_name       = "tfstate"
    key                  = "iothub.tfstate"
  }
  required_version = ">=0.13"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~>2.0"
    }
  }
}

provider "azurerm" {
  features {}
}
