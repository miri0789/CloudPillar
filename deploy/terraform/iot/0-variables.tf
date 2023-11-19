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

