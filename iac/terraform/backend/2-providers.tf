# Define Terraform provider
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=2.46.0"
    }
  }
  required_version = ">= 0.13"
}

// terraform {
//   backend "local" {
//     path = "terraform.tfstate.d/dev/terraform.tfstate"
//   }
//   required_providers {
//     azurerm = {
//       source  = "hashicorp/azurerm"
//       version = "=2.46.0"
//     }
//   }
//   required_version = ">= 0.13"
// }

# Configure the Azure provider
provider "azurerm" {
  subscription_id = var.subscription_id
  tenant_id = var.tenant_id
  client_id = var.client_id
  client_secret = var.client_secret
  features {}
}