terraform {
  backend "azurerm" {
    subscription_id      = "a147112f-bc59-4e9e-ac2f-5b4585e6542e"
    resource_group_name  = "cp-tf-rg"
    storage_account_name = "tfcp"
    container_name       = "tfstate"
    key                  = "iot.tfstate"
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
  subscription_id = "b212f5cf-93c9-4509-ad51-6d986c492788"
}