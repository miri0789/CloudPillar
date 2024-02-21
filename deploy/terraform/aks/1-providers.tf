terraform {
  backend "azurerm" {
    subscription_id      = "a147112f-bc59-4e9e-ac2f-5b4585e6542e"
    resource_group_name  = "cp-tf-rg"
    storage_account_name = "tfcp"
    container_name       = "tfstate"
    key                  = "aks.tfstate"
  }
  required_version = ">=0.12"

   required_providers {
   azuread = {
      source  = "hashicorp/azuread"
      version = "~> 2.47.0"
    }
    azapi = {
      source  = "Azure/azapi"
      version = "1.8.0"
    }
  }
}
provider "azuread" {
  tenant_id = "63d53a16-04d5-4981-b530-4f38d3b16281"
}

provider "azurerm" {
  features {}
  subscription_id = "a147112f-bc59-4e9e-ac2f-5b4585e6542e"
}