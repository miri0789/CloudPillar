variable "location" {
  type = string
  description = "The Azure location where all resources should be created"
  default = "westeurope"
}
variable "env" {
  type = string
  description = "The environment to assign the resources"
  default = "dev"
}
variable "akspat" {
  type = string
}
variable "addressSpace"{
  description = "The subnet address prefixes of aks vnet"
  default = "10.23.1.1/24"
}

variable "AgreedBy_IP_v6" { 
  type = string
  default = "147.234.64.52"
}

variable "AgreedAt_DateTime" {  
  type = string
  default = "2024-02-15T10:30:59.264Z"
}

variable "contact" {
  type = object({
    nameFirst = string
    nameLast  = string
    email     = string
    phone     = string
    addressMailing = object({
      address1   = string
      city       = string
      state      = string
      country    = string
      postalCode = string
    })
  })
  default= {
  nameFirst = "Sergey"
    nameLast  = "Zolotnitsky"
    email     = "szolotni@its.jnj.com"
    phone     = "+972.0542300303"
    addressMailing = {
      address1   = "Hatnufa 4"
      city       = "Yokneham Ilit"
      state      = "IL"
      country    = "IL"
      postalCode = "2066717"
  }
  }
}