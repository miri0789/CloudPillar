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
variable "keyVaultName" {
  type = string
  default = ""
}
variable "keyVaultRG" {
  type = string
  default = ""
}