variable "env" {
  type        = string
  description = "The environment to assign the resources"
  default     = "test"
}

variable "location" {
  type        = string
  description = "The resource phisical location"
  default     = "eastus"
}

variable "ip-pfx" {
  type        = string
  description = "Prefix of k8s vnet ip address space"
  default     = "10" # For dev
}