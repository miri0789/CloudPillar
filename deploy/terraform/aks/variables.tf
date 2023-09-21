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
variable "akspat" {
  type = string
  default = "qkgtqnqlmx5lwves7ja7cilb5vii4q46slk2kxyfr6cmptmp37hq"
}