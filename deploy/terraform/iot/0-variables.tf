variable "location" {
  type = string
  description = "The Azure location where all resources should be created"
  default = "West Europe"
}
variable "env" {
  type = string
  description = "The environment to assign the resources"
  default = "dev"
}
variable "subscription_id" {
  type = string
  default = "a147112f-bc59-4e9e-ac2f-5b4585e6542e"
}