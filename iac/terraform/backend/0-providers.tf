# Define Terraform provider
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~>3.0"
    }
  }
  required_version = ">= 0.13"
}

# Configure the Azure provider
provider "azurerm" { 
  features {}  
  subscription_id = ""
  skip_provider_registration = true
}
