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